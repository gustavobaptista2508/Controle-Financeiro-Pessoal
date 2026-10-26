"""Mescla UFs coletadas, sem tratar falta de arquivo como zero votos."""
from __future__ import annotations
from datetime import datetime,timezone
from pathlib import Path
import json
from coletor_tse import UF_REGIAO


def merge(root:Path,output:Path):
    states={}; sections=[]; rows=[]
    for uf,region in sorted(UF_REGIAO.items()):
        path=root/f'{uf}.json'
        if not path.exists():
            states[uf]={'uf':uf,'region':region,'status':'pending'}
            continue
        try:
            dat=json.loads(path.read_text(encoding='utf-8'))
            if dat.get('source')!='TSE-LOGS-2026-2T' or dat.get('uf')!=uf or dat.get('schema')!=1:
                raise ValueError('Cabeçalho incompatível')
            if dat.get('election_date')!='2026-10-25':raise ValueError('Data incompatível')
            allsec=dat['sections']
            if not allsec or not isinstance(allsec,list):raise ValueError('Sem seções')
            expected=int(dat['coverage']['expected_log_files']);processed=len(allsec)
            if processed>expected:raise ValueError('Cobertura inconsistente')
            if sum(s['total_votos'] for s in allsec)!=dat['total_votes_logged']:raise ValueError('Votos totais divergentes')
            if sum(s['votos_apos_17'] for s in allsec)!=dat['after_17']:raise ValueError('Votos 17h divergentes')
            groups={}
            for s in allsec:
                key=(s['codigo_municipio'],s['municipio']);x=groups.setdefault(key,{'uf':uf,'regiao':region,'municipio':s['municipio'],'codigo_municipio':s['codigo_municipio'],'sections':0,'total':0,'after':0,'bands':[0]*5})
                x['sections']+=1;x['total']+=s['total_votos'];x['after']+=s['votos_apos_17']
                for i,k in enumerate(['apos_17_1730','apos_1730_18','apos_18_1830','apos_1830_19','apos_19']):x['bands'][i]+=s[k]
            rows.extend(groups.values())
            states[uf]={'uf':uf,'region':region,'status':'partial' if processed<expected else 'complete',
                        'sections':processed,'expected':expected,'votes':dat['total_votes_logged'],
                        'after':dat['after_17'],'generated_at':dat.get('generated_at')}
            sections.extend(allsec)
        except (ValueError,KeyError,TypeError,IndexError) as e:
            states[uf]={'uf':uf,'region':region,'status':'invalid','error':str(e)[:120]}
    data={'source':'TSE-LOGS-2026-2T','schema':2,'election_date':'2026-10-25',
          'generated_at':datetime.now(timezone.utc).isoformat(),
          'method':'Contagem de eventos VOTA/O voto do eleitor foi computado (>=17h BRT). Não identifica candidato nem irregularidade.',
          'source_dataset':'https://dadosabertos.tse.jus.br/dataset/resultados-2026-arquivos-transmitidos-para-totalizacao',
          'coverage':{'ufs_present':sum(x['status'] in ('complete','partial') for x in states.values()),
                      'ufs_expected':len(states),'sections_processed':len(sections),'sections_expected_with_logs':sum(x.get('expected',0) for x in states.values()),
                      'states':states},
          'total_votacoes_analisadas':sum(x['total'] for x in rows),
          'votacoes_apos_17':sum(x['after'] for x in rows),
          'municipalities':rows}
    output.parent.mkdir(parents=True,exist_ok=True)
    if output.exists():
        try:
            previous=json.loads(output.read_text(encoding='utf-8'))
            comparable={k:v for k,v in previous.items() if k!='generated_at'}
            incoming={k:v for k,v in data.items() if k!='generated_at'}
            if comparable==incoming: return previous  # Evita commit diário sem mudanças
        except (ValueError,OSError):pass
    output.write_text(json.dumps(data,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
    return data

if __name__=='__main__':
    result=merge(Path('data/ufs'),Path('data/auditoria17h_2026_2t.json'))
    print('UFs',result['coverage']['ufs_present'],'seções',result['coverage']['sections_processed'],
          'votos após 17h',result['votacoes_apos_17'])
