import io
import json
import tempfile
from pathlib import Path
import unittest
import zipfile
import sys
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from coletor_tse import parse_log,collect,name_map_from_json
from agregador import merge


def make_log(times,opening='08:00:05'):
    events=[f'25/10/2026 {opening}\tINFO\t123\tVOTA\tAguardando digitação do identificador do eleitor\tHASH']
    events.extend(f'25/10/2026 {t}\tINFO\t123\tVOTA\tO voto do eleitor foi computado\tHASH' for t in times)
    events.append('04/10/2026 18:00:00\tINFO\t123\tVOTA\tO voto do eleitor foi computado\tHASH')
    buff=io.BytesIO()
    with zipfile.ZipFile(buff,'w') as z:z.writestr('logd.dat','\n'.join(events).encode('latin-1'))
    return buff.getvalue()

class Tests(unittest.TestCase):
    def test_bounds(self):
        p=parse_log(make_log(['16:59:59','17:00:00','17:29:59','17:30:00','18:15:00','18:59:59','19:00:00']),'SP')
        self.assertEqual((p['total'],p['after'],p['bands']),(7,6,[2,1,1,1,1]))
    def test_acre_local_clock(self):
        p=parse_log(make_log(['14:59:59','15:00:00'],opening='06:00:05'),'AC')
        self.assertEqual(p['after'],1)
        self.assertEqual(p['timezone_offset'],-2)
    def test_other_day_not_count(self):
        self.assertEqual(parse_log(make_log(['08:20:00']),'ES')['total'],1)
    def test_nested_zip_and_merge(self):
        with tempfile.TemporaryDirectory() as td:
            root=Path(td); zp=root/'tse.zip';ufout=root/'data'/'ufs'/'ES.json'
            with zipfile.ZipFile(zp,'w') as z:
                z.writestr('folder/o03221es1234500010002-log.jez',make_log(['16:00:00','17:01:00']))
                z.writestr('folder/unrelated.bu',b'irrelevant')
            raw=collect('ES',ufout,local_zip=zp)
            self.assertEqual(raw['after_17'],1)
            self.assertEqual(raw['sections'][0]['codigo_municipio'],'12345')
            summary=merge(root/'data'/'ufs',root/'data'/'summary.json')
            self.assertEqual(summary['coverage']['ufs_present'],1)
            self.assertEqual(summary['coverage']['ufs_expected'],27)
            self.assertEqual(summary['votacoes_apos_17'],1)
            self.assertEqual(summary['municipalities'][0]['sections'],1)
    def test_name_map(self):
        self.assertEqual(name_map_from_json({'abc':[{'cd':'12345','nm':'Exemplo'}]})['12345'],'Exemplo')

if __name__=='__main__': unittest.main()
