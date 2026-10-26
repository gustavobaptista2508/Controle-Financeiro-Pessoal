"""Coletor independente para logs públicos do TSE no segundo turno de 2026.

Processa eventos de voto, não preferências do eleitor. Não associa horários a candidatos.
Os ZIPs do TSE podem ser gigantes: por padrão usa HTTP Range e lê só os logs.
"""
from __future__ import annotations

import argparse
from collections import Counter, defaultdict, OrderedDict
from datetime import datetime, timezone
import io
import json
import logging
from pathlib import Path
import re
import struct
import time
from typing import BinaryIO
import urllib.request
import urllib.error
import zipfile
import zlib

URL = ('https://cdn.tse.jus.br/estatistica/sead/eleicoes/eleicoes2026/'
       'arqurnatot/bu_imgbu_logjez_rdv_vscmr_2026_2t_{uf}.zip')
TSE_MUNICIPIOS = ('https://resultados.tse.jus.br/oficial/ele2026/6258/'
                  'config/mun-e006258-cm.json')
DIA_ELEICAO = '25/10/2026'
UF_REGIAO = {
    'AC':'Norte','AP':'Norte','AM':'Norte','PA':'Norte','RO':'Norte','RR':'Norte','TO':'Norte',
    'AL':'Nordeste','BA':'Nordeste','CE':'Nordeste','MA':'Nordeste','PB':'Nordeste','PE':'Nordeste','PI':'Nordeste','RN':'Nordeste','SE':'Nordeste',
    'DF':'Centro-Oeste','GO':'Centro-Oeste','MT':'Centro-Oeste','MS':'Centro-Oeste',
    'ES':'Sudeste','MG':'Sudeste','RJ':'Sudeste','SP':'Sudeste',
    'PR':'Sul','RS':'Sul','SC':'Sul',
}
FUSO = {'AC':-2, 'AM':-1, 'MT':-1, 'MS':-1, 'RO':-1, 'RR':-1}
LOG_NAME = re.compile(r'(\d{5})(\d{4})(\d{4})-log\.jez$', re.IGNORECASE)
VOTO = 'O voto do eleitor foi computado'
PRONTA = ('Aguardando digitação do identificador do eleitor', 'Aguardando digitação do título')
CLIENT = 'BI-Eleicoes-2026/1.0 (dados-publicos; respeita-limites)'
LOG = logging.getLogger('tse')


def request_range(url: str, begin: int, end: int, attempts: int = 5) -> bytes:
    """Não aceitar resposta 200: impedir download acidental de gigabytes."""
    for attempt in range(attempts):
        request = urllib.request.Request(url, headers={
            'User-Agent': CLIENT, 'Range': f'bytes={begin}-{end}', 'Accept-Encoding':'identity'})
        try:
            with urllib.request.urlopen(request, timeout=90) as resp:
                if resp.status != 206:
                    raise RuntimeError(f'Servidor sem HTTP Range: {resp.status}')
                content = resp.read(end - begin + 2)
                if len(content) != end - begin + 1:
                    raise IOError(f'Resposta incompleta: {len(content)} bytes')
                return content
        except urllib.error.HTTPError as exc:
            if exc.code in (403,404,416):
                raise
            if attempt == attempts - 1:
                raise
        except (TimeoutError, OSError, IOError) as exc:
            if attempt == attempts - 1:
                raise
            LOG.warning('Tentativa %s falhou: %s', attempt + 1, exc)
        time.sleep(min(2 ** attempt, 12))
    raise RuntimeError('Falha de HTTP Range')


def remote_length(url: str) -> int:
    req = urllib.request.Request(url, headers={
        'User-Agent': CLIENT, 'Range': 'bytes=0-0', 'Accept-Encoding':'identity'})
    with urllib.request.urlopen(req, timeout=90) as resp:
        if resp.status != 206:
            raise RuntimeError('Fonte não oferece downloads parciais HTTP Range')
        part = resp.headers.get('Content-Range','')
        if not re.fullmatch(r'bytes 0-0/\d+', part):
            raise RuntimeError('Content-Range inválido: '+part)
        return int(part.rsplit('/',1)[1])


class RemoteZip(io.RawIOBase):
    def __init__(self,url: str,size:int,block_size:int=1<<20,cache_blocks:int=12):
        self.url=url; self.size=size; self.block_size=block_size
        self.offset=0; self.cache_blocks=cache_blocks; self.cache=OrderedDict()
    def readable(self): return True
    def seekable(self): return True
    def tell(self): return self.offset
    def seek(self,offset:int,whence:int=0):
        self.offset=offset+(self.offset if whence==1 else self.size if whence==2 else 0)
        if self.offset<0: raise ValueError('seek negativo')
        return self.offset
    def read(self,n:int=-1):
        if n is None or n<0: n=self.size-self.offset
        n=min(n,self.size-self.offset)
        out=bytearray()
        while n>0:
            block, start = divmod(self.offset,self.block_size)
            if block not in self.cache:
                ini=block*self.block_size
                self.cache[block]=request_range(self.url,ini,min(self.size,ini+self.block_size)-1)
                if len(self.cache)>self.cache_blocks:self.cache.popitem(last=False)
            else:self.cache.move_to_end(block)
            chunk=self.cache[block][start:start+n]
            if not chunk:raise IOError('Bloco incompleto do ZIP remoto')
            out.extend(chunk);self.offset+=len(chunk);n-=len(chunk)
        return bytes(out)


def read_member(url: str, info: zipfile.ZipInfo) -> bytes:
    """Lê apenas um membro do ZIP remoto com CRC de integridade obrigatório."""
    at=info.header_offset
    header=request_range(url,at,at+29)
    if header[:4]!=b'PK\x03\x04':raise ValueError('Cabeçalho ZIP local inválido')
    filename_len,extra_len=struct.unpack('<HH',header[26:30])
    start=at+30+filename_len+extra_len
    if info.compress_size>50*1024*1024:raise ValueError('Log comprimido excede limite individual (50MB)')
    payload=request_range(url,start,start+info.compress_size-1) if info.compress_size else b''
    if info.compress_type==zipfile.ZIP_STORED:data=payload
    elif info.compress_type==zipfile.ZIP_DEFLATED:data=zlib.decompress(payload,-15)
    else:raise ValueError('Algoritmo ZIP não suportado: '+str(info.compress_type))
    if len(data)!=info.file_size or (zlib.crc32(data)&0xffffffff)!=info.CRC:
        raise ValueError('Verificação CRC/tamanho falhou: '+info.filename)
    return data


def parse_log(binary: bytes,uf:str) -> dict:
    """Apenas eventos VOTA do dia 25/10, sem dados de preferência ou identificação."""
    with zipfile.ZipFile(io.BytesIO(binary)) as archive:
        entries=[i for i in archive.infolist() if i.filename.lower().endswith('logd.dat')]
        if len(entries)!=1:raise ValueError('Esperado um único logd.dat')
        if entries[0].file_size>80*1024*1024:raise ValueError('logd.dat excessivamente grande')
        data=archive.read(entries[0]).decode('latin-1')
    times=[]; opening=None
    for line in data.splitlines():
        fields=line.split('\t')
        if len(fields)<5 or fields[3]!='VOTA' or not fields[0].startswith(DIA_ELEICAO+' '):continue
        ts=fields[0][11:19]
        if not re.fullmatch(r'\d{2}:\d{2}:\d{2}',ts):continue
        h,m,s=map(int,ts.split(':'))
        if h>23 or m>59 or s>59:continue
        seconds=h*3600+m*60+s
        if fields[4]==VOTO:times.append(seconds)
        if fields[4] in PRONTA and opening is None:opening=seconds
    # A urna registra horário LOCAL, não BRT. Inferir exceções apenas quando
    # a abertura ficou a <= 30 minutos de um horário esperado (06/07/08/09h).
    offset=FUSO.get(uf,0); inference='uf_default'
    if opening is not None:
        candidates=[-2,-1,0,1]
        ranking=sorted(candidates,key=lambda x:abs(opening-(8+x)*3600))
        best=ranking[0]
        if abs(opening-(8+best)*3600)<=30*60:
            offset=best;inference='hora_abertura'
    bins=[0]*5
    for seconds in times:
        brt=seconds-offset*3600
        if brt>=17*3600:
            band=0 if brt<17*3600+30*60 else 1 if brt<18*3600 else 2 if brt<18*3600+30*60 else 3 if brt<19*3600 else 4
            bins[band]+=1
    return {'total':len(times),'after':sum(bins),'bands':bins,'timezone_offset':offset,'fuso_inference':inference}


def name_map_from_json(payload: object) -> dict[str,str]:
    """Reconhece a estrutura oficial EA12 sem inventar nomes."""
    found={}
    def visit(node):
        if isinstance(node,dict):
            # TSE: cd (municipio código TSE), nm (nome)
            code=node.get('cd'); name=node.get('nm')
            if code is not None and isinstance(name,str) and name.strip():
                k=str(code).zfill(5)
                if re.fullmatch(r'\d{5}',k):found[k]=name.strip()
            for val in node.values():
                if isinstance(val,(dict,list)):visit(val)
        elif isinstance(node,list):
            for item in node:visit(item)
    visit(payload)
    return found


def download_name_map() -> dict[str,str]:
    try:
        with urllib.request.urlopen(urllib.request.Request(TSE_MUNICIPIOS,headers={'User-Agent':CLIENT}),timeout=35) as r:
            if int(r.headers.get('Content-Length','0'))>12*1024*1024:raise ValueError('Config maior que limite')
            body=r.read(12*1024*1024+1)
        if len(body)>12*1024*1024:raise ValueError('Config maior que limite')
        return name_map_from_json(json.loads(body))
    except Exception as ex:
        LOG.warning('Nomes do TSE indisponíveis; exibindo código do município: %s',ex)
        return {}


def collect(uf:str,out:Path,local_zip:Path|None=None,limit:int|None=None) -> dict:
    if uf not in UF_REGIAO:raise ValueError('UF inválida')
    if local_zip:
        zr=zipfile.ZipFile(local_zip)
        infos=zr.infolist()
        get=lambda info: zr.read(info)
        source='fixture local'
    else:
        source=URL.format(uf=uf)
        length=remote_length(source)
        with zipfile.ZipFile(RemoteZip(source,length)) as zr:infos=zr.infolist()
        get=lambda info:read_member(source,info)
    logs=[info for info in infos if info.filename.lower().endswith(('-log.jez','.logjez'))]
    if not logs:raise ValueError('Nenhum log encontrado no ZIP (não publicar zeros)')
    if limit:logs=logs[:limit]
    rows={};failures=[];duplicates=0; duplicate_keys=set();fuso_fallback=0
    mun_names=download_name_map() if not local_zip else {}
    for index,info in enumerate(logs,1):
        filename=info.filename.split('/')[-1]
        match=LOG_NAME.search(filename)
        if not match:
            failures.append({'filename':filename,'error':'identificação de seção inválida'});continue
        cd,zona,secao=match.groups()
        key=(cd,zona,secao)
        if key in rows or key in duplicate_keys:
            failures.append({'filename':filename,'error':'duplicata da mesma seção'});duplicates+=1
            # Não contam duas vezes; também exclui todos os registros conflitantes.
            rows.pop(key,None);duplicate_keys.add(key);continue
        try:
            p=parse_log(get(info),uf)
        except Exception as ex:
            failures.append({'filename':filename,'error':str(ex)[:170]});continue
        if p['fuso_inference']=='uf_default': fuso_fallback+=1
        rows[key]={'uf':uf,'municipio':mun_names.get(cd,f'Código TSE {cd}'),
                   'codigo_municipio':cd,'zona':str(int(zona)),'secao':str(int(secao)),
                   'turno':2,'total_votos':p['total'],'votos_apos_17':p['after'],
                   'apos_17_1730':p['bands'][0],'apos_1730_18':p['bands'][1],
                   'apos_18_1830':p['bands'][2],'apos_1830_19':p['bands'][3],
                   'apos_19':p['bands'][4]}
        if index%500==0:LOG.info('%s processadas %s/%s; erros=%s',uf,index,len(logs),len(failures))
    if not rows:raise ValueError('Nenhuma seção validamente processada')
    total=sum(r['total_votos'] for r in rows.values());after=sum(r['votos_apos_17'] for r in rows.values())
    output={'source':'TSE-LOGS-2026-2T','schema':1,'uf':uf,'region':UF_REGIAO[uf],
       'generated_at':datetime.now(timezone.utc).isoformat(),
       'election_date':'2026-10-25','source_url':source,
       'coverage':{'expected_log_files':len(logs),'processed_sections':len(rows),
                   'excluded':len(failures),'duplicate_encounters':duplicates,
                   'timezone_default_sections':fuso_fallback},
       'total_votes_logged':total,'after_17':after,
       'sections':list(rows.values()),'warnings':failures[:30]}
    out.parent.mkdir(parents=True,exist_ok=True)
    target=out.with_suffix(out.suffix+'.tmp')
    target.write_text(json.dumps(output,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
    target.replace(out)
    LOG.info('%s: %s seções, %s votações, %s após 17h, falhas=%s',uf,len(rows),total,after,len(failures))
    return output


def main():
    p=argparse.ArgumentParser()
    p.add_argument('--uf',required=True,choices=sorted(UF_REGIAO))
    p.add_argument('--output',default=None)
    p.add_argument('--zip-local',type=Path)
    p.add_argument('--limit',type=int)
    args=p.parse_args()
    logging.basicConfig(level=logging.INFO,format='%(levelname)s %(message)s')
    path=Path(args.output) if args.output else Path('data/ufs')/f'{args.uf}.json'
    collect(args.uf,path,args.zip_local,args.limit)

if __name__=='__main__':main()
