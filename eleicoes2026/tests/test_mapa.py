import io,os,sys,tempfile,unittest,zipfile
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from gerar_mapa_viradas import read_tse_archive,build_rows,aggregate

class MapaTests(unittest.TestCase):
 def test_turnos_separados_geocodificados(self):
  data='SG_UF;CD_MUNICIPIO;NM_MUNICIPIO;NR_TURNO;NR_CANDIDATO;QT_VOTOS_NOMINAIS;CD_CARGO\n'+\
    'SP;99999;Cidade Exemplo;1;13;40;1\nSP;99999;Cidade Exemplo;1;22;60;1\n'+\
    'SP;99999;Cidade Exemplo;2;13;90;1\nSP;99999;Cidade Exemplo;2;22;70;1\n'
  with tempfile.TemporaryDirectory() as td:
   p=Path(td)/'in.zip'
   with zipfile.ZipFile(p,'w') as z:z.writestr('votacao_candidato_munzona_2026_BR.csv',data.encode('latin-1'))
   votes,labels,meta=read_tse_archive(p)
   rows,issues=build_rows(votes,labels,{('SP','99999'):(-23.5,-46.6)},{})
   self.assertEqual(len(rows),1)
   self.assertTrue(rows[0]['virada_finalistas'])
   self.assertTrue(rows[0]['virada_geral'])
   self.assertEqual(rows[0]['variacao_margem'],40)
   self.assertEqual(aggregate(rows)['finalistas']['para_lula'],1)
 def test_terceiro_candidato_lidera_primeiro_turno(self):
  v={(('RJ','123'),'1'):{'13':30,'22':45,'15':90},(('RJ','123'),'2'):{'13':60,'22':55}}
  rows,_=build_rows(v,{('RJ','123'):'Exemplo'},{('RJ','123'):(-22.9,-43.2)},{})
  self.assertTrue(rows[0]['virada_geral'])
  self.assertTrue(rows[0]['virada_finalistas'])
  self.assertEqual(rows[0]['lider_1t_geral_numero'],'15')
 def test_sem_virada_quando_lider_mantem(self):
  v={(('ES','111'),'1'):{'13':15,'22':45},(('ES','111'),'2'):{'13':25,'22':48}}
  rows,_=build_rows(v,{('ES','111'):'Exemplo'},{('ES','111'):(-20,-40)},{})
  self.assertFalse(rows[0]['virada_geral'])
  self.assertFalse(rows[0]['virada_finalistas'])

if __name__=='__main__':unittest.main()
