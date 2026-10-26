#!/usr/bin/env python3
"""Gera JSON auditável de viradas presidenciais por município, usando apenas dados oficiais TSE.
Não estima intenção de voto, não associa votos a eleitores e não infere irregularidades.
"""
import argparse
from collections import defaultdict
import csv
from datetime import datetime, timezone
import io
import json
import os
from pathlib import Path
import re
import tempfile
import unicodedata
import zipfile
import requests

TSE_URL = 'https://cdn.tse.jus.br/estatistica/sead/odsele/votacao_candidato_munzona/votacao_candidato_munzona_2026.zip'
CROSSWALK_URL = 'https://raw.githubusercontent.com/betafcc/Municipios-Brasileiros-TSE/master/municipios_brasileiros_tse.csv'
GEOCODE_URL = 'https://raw.githubusercontent.com/kelvins/municipios-brasileiros/main/csv/municipios.csv'
REGIONS = {'AC':'Norte','AP':'Norte','AM':'Norte','PA':'Norte','RO':'Norte','RR':'Norte','TO':'Norte',
           'AL':'Nordeste','BA':'Nordeste','CE':'Nordeste','MA':'Nordeste','PB':'Nordeste','PE':'Nordeste','PI':'Nordeste','RN':'Nordeste','SE':'Nordeste',
           'DF':'Centro-Oeste','GO':'Centro-Oeste','MT':'Centro-Oeste','MS':'Centro-Oeste',
           'ES':'Sudeste','MG':'Sudeste','RJ':'Sudeste','SP':'Sudeste',
           'PR':'Sul','RS':'Sul','SC':'Sul'}
VALID_UFS = set(REGIONS)
FINALISTS = {'13':'Lula', '22':'Flávio'}


def normal_name(s):
    return ''.join(c for c in unicodedata.normalize('NFD', str(s).upper()) if not unicodedata.combining(c)).strip()


def read_remote_csv(url, session):
    r=session.get(url,timeout=(15,90)); r.raise_for_status()
    content=r.content.decode('utf-8-sig')
    return list(csv.DictReader(io.StringIO(content)))


def geo_lookup(session):
    tse=read_remote_csv(CROSSWALK_URL,session)
    coord=read_remote_csv(GEOCODE_URL,session)
    by_ibge={str(x['codigo_ibge']): (float(x['latitude']),float(x['longitude'])) for x in coord}
    by_tse={}
    by_name={}
    for x in tse:
        uf=x['uf'].strip().upper(); key=(uf,str(int(x['codigo_tse'])))
        p=by_ibge.get(x['codigo_ibge'].strip())
        if p:
            by_tse[key]=p
            by_name[(uf,normal_name(x['nome_municipio']))]=p
    return by_tse,by_name


def fetch_zip(session,destination):
    """Download único sem manter megabytes em RAM; recomeça apenas em falha."""
    destination=Path(destination)
    for attempt in range(3):
        try:
            with session.get(TSE_URL,stream=True,timeout=(25,180),headers={'User-Agent':'BI-Eleicoes-2026/2.0 (dados-publicos)'}) as r:
                if r.status_code==404:raise FileNotFoundError('O TSE não disponibiliza o ZIP de resultados no endereço oficial neste momento (404).')
                r.raise_for_status()
                with destination.open('wb') as fp:
                    for chunk in r.iter_content(2*1024*1024):
                        if chunk: fp.write(chunk)
            with zipfile.ZipFile(destination) as z:z.testzip() if os.environ.get('TSE_VERIFY_ARCHIVE_FULL')=='1' else z.namelist()
            return destination
        except (OSError,requests.RequestException,zipfile.BadZipFile) as exc:
            if attempt==2:raise RuntimeError(f'Falha ao obter ZIP oficial do TSE: {exc}') from exc
    return destination


def find_col(names, candidates):
    available={c.upper().strip():c for c in names}
    for k in candidates:
        if k in available:return available[k]
    raise ValueError(f'CSV TSE sem coluna obrigatória {candidates}. Colunas: {names[:25]}')


def read_tse_archive(path):
    """Retorna votos por município/turno/candidato. Evita duplicar arquivo BR + arquivos estaduais."""
    votes=defaultdict(lambda:defaultdict(int))
    labels={}
    with zipfile.ZipFile(path) as z:
        entries=[n for n in z.namelist() if n.lower().endswith('.csv') and 'votacao_candidato_munzona_2026_' in n.lower()]
        nat=[n for n in entries if re.search(r'_(?:BR|BRASIL)\.csv$',n,re.I)]
        selected=nat[:1] if nat else [n for n in entries if re.search(r'_[A-Z]{2}\.csv$',n,re.I)]
        if not selected:raise ValueError(f'Nenhum arquivo nacional ou estadual de votação reconhecido no ZIP: {z.namelist()[:15]}')
        processed=0
        for entry in selected:
            with z.open(entry) as binary:
                reader=csv.DictReader(io.TextIOWrapper(binary,encoding='latin-1',newline=''),delimiter=';')
                field=reader.fieldnames or []
                ufkey=find_col(field,['SG_UF'])
                munkey=find_col(field,['CD_MUNICIPIO'])
                namekey=find_col(field,['NM_MUNICIPIO'])
                turnkey=find_col(field,['NR_TURNO'])
                candidatekey=find_col(field,['NR_CANDIDATO'])
                countkey=find_col(field,['QT_VOTOS_NOMINAIS','QT_VOTOS_NOMINAIS_VALIDOS'])
                cargo=find_col(field,['CD_CARGO','DS_CARGO'])
                for row in reader:
                    if str(row[cargo]).strip().upper() not in ('1','PRESIDENTE'):continue
                    uf=row[ufkey].strip().upper()
                    if uf not in VALID_UFS:continue  # exterior não é município brasileiro
                    turno=str(row[turnkey]).strip()
                    if turno not in ('1','2'):continue
                    code=str(row[munkey]).strip()
                    if not code.isdigit():continue
                    cid=str(int(code))
                    candidate=str(row[candidatekey]).strip()
                    if not candidate.isdigit():continue
                    candidate=str(int(candidate))
                    q=str(row[countkey]).strip()
                    if not q.isdigit():continue
                    key=(uf,cid)
                    votes[(key,turno)][candidate]+=int(q)
                    labels[key]=row[namekey].strip()
                    processed+=1
        return votes,labels,{'csv_members':selected,'processed_rows':processed}


def winner(votes):
    if not votes:return None
    ranked=sorted(votes.items(),key=lambda x:-x[1]);
    if len(ranked)>1 and ranked[0][1]==ranked[1][1]:return None
    return ranked[0][0]


def build_rows(votes,labels,geo_by_tse,geo_by_name):
    all_keys=sorted(labels)
    ready=[];without_geo=0;without_turn=0;ties=0
    for key in all_keys:
        uf,code=key
        v1=votes.get((key,'1'),{})
        v2=votes.get((key,'2'),{})
        if not v1 or not v2:
            without_turn+=1;continue
        # O primeiro turno geral usa todos os candidatos; o primeiro entre finalistas usa só os números 13 e 22.
        lead1=winner(v1)
        f1=winner({k:v1.get(k,0) for k in FINALISTS})
        f2=winner({k:v2.get(k,0) for k in FINALISTS})
        if not lead1 or not f1 or not f2:
            ties+=1;continue
        latlon=geo_by_tse.get(key) or geo_by_name.get((uf,normal_name(labels[key])))
        if not latlon:without_geo+=1
        m1=v1.get('13',0)-v1.get('22',0)
        m2=v2.get('13',0)-v2.get('22',0)
        record={'uf':uf,'regiao':REGIONS[uf],'cd_municipio':code,'municipio':labels[key],
             'latitude':round(latlon[0],5) if latlon else None,'longitude':round(latlon[1],5) if latlon else None,
             'lula_1t':v1.get('13',0),'flavio_1t':v1.get('22',0),
             'lula_2t':v2.get('13',0),'flavio_2t':v2.get('22',0),
             'lider_1t_geral_numero':lead1,'lider_1t_finalistas':FINALISTS[f1],
             'lider_2t':FINALISTS[f2],
             'virada_geral':lead1!=f2,'virada_finalistas':f1!=f2,
             'margem_1t':m1,'margem_2t':m2,'variacao_margem':m2-m1}
        ready.append(record)
    return ready,{'without_geocode':without_geo,'without_complete_turns':without_turn,'tied_or_missing_leaders':ties,'municipalities_with_votes':len(all_keys)}


def aggregate(rows):
    out={mode:{'total':0,'para_lula':0,'para_flavio':0,'por_regiao':{},'por_uf':{}} for mode in ('geral','finalistas')}
    for mode in out:
        count=out[mode]
        for row in rows:
            if not row['virada_'+mode]:continue
            count['total']+=1
            to='para_lula' if row['lider_2t']=='Lula' else 'para_flavio'
            count[to]+=1
            for dim,value in [('por_regiao',row['regiao']),('por_uf',row['uf'])]:
                group=count[dim].setdefault(value,{'total':0,'para_lula':0,'para_flavio':0});group['total']+=1;group[to]+=1
    return out


def build_dataset(zip_path,session):
    by_tse,by_name=geo_lookup(session)
    votes,labels,meta=read_tse_archive(zip_path)
    n1=len({key for key,t in votes if t=='1'})
    n2=len({key for key,t in votes if t=='2'})
    if n1<5000 or n2<5000:raise ValueError(f'Dados incompletos: 1º turno {n1} municípios, 2º turno {n2}. Não publicar parcial como total final.')
    rows,issues=build_rows(votes,labels,by_tse,by_name)
    if len(rows)<5000:raise ValueError(f'Consolidação incompleta: {len(rows)} municípios; publicação suspensa.')
    result={'schema':'viradas-municipais-2026-v1','source':'TSE_RESULTADOS_2026','generated_at':datetime.now(timezone.utc).isoformat(),
            'elections':{'first':'2026-10-04','second':'2026-10-25'},
            'source_url':TSE_URL,'coordinates_source':[CROSSWALK_URL,GEOCODE_URL],
            'methodology':'Virada_geral: líder entre todos os candidatos no 1º turno contra líder no 2º. Virada_finalistas: líder entre 13 e 22 em cada turno. Empates excluídos.',
            'coverage':{'municipalities_first':n1,'municipalities_second':n2,'municipalities_compared':len(rows),**issues},
            'count':aggregate(rows),'municipios':rows,'meta':meta}
    return result


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--zip',help='Caminho de ZIP local de resultados do TSE (somente para teste/reprocessamento)')
    parser.add_argument('--out',default='eleicoes2026/data/viradas_municipios_2026.json')
    args=parser.parse_args()
    session=requests.Session()
    zip_path=args.zip
    if not zip_path:
        with tempfile.TemporaryDirectory(prefix='tse-2026-') as td:
            zip_path=fetch_zip(session,Path(td)/'votacao_munzona_2026.zip')
            result=build_dataset(zip_path,session)
    else:result=build_dataset(zip_path,session)
    output=Path(args.out);output.parent.mkdir(parents=True,exist_ok=True)
    temp=output.with_suffix('.tmp')
    temp.write_text(json.dumps(result,ensure_ascii=False,separators=(',',':'))+'\n',encoding='utf-8')
    temp.replace(output)
    print('Publicado',len(result['municipios']),'municípios; contagens',json.dumps(result['count'],ensure_ascii=False)[:450])


if __name__=='__main__':main()
