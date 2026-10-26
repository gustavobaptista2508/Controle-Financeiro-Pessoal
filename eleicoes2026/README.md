# BI Eleições 2026 — auditoria automática dos logs TSE

## Objetivo

Contar **eventos de votação computados a partir das 17h, horário de Brasília**, por seção, município, UF e região no 2º turno de 25/10/2026, a partir dos arquivos públicos do TSE. **Não associa horários a candidatos, não identifica eleitores e não conclui fraude ou prejuízo eleitoral.** Eleitores já presentes na fila às 17h podiam votar depois desse horário.

## Fontes e integridade

- Dados abertos TSE: https://dadosabertos.tse.jus.br/dataset/resultados-2026-arquivos-transmitidos-para-totalizacao
- URL de pacote por UF: `https://cdn.tse.jus.br/estatistica/sead/eleicoes/eleicoes2026/arqurnatot/bu_imgbu_logjez_rdv_vscmr_2026_2t_<UF>.zip` (o recurso pode não ter sido publicado ainda).
- O coletor lê o ZIP remoto por **HTTP Range**, sem transferir os demais arquivos, e verifica tamanho e CRC-32 de cada log antes de extrair o `logd.dat` interno.
- Apenas linhas `VOTA` de **25/10/2026** cuja mensagem seja exatamente `O voto do eleitor foi computado` entram na contagem.
- Logs trazem relógio **local**. O código usa deslocamentos padrão das UFs e, quando há evidência, o início efetivo da urna para corrigir exceções. `timezone_default_sections` informa quantas seções usaram apenas o fuso presumido; isso exige cautela em regiões com fuso excepcional.
- Falhas e duplicações são excluídas e informadas como cobertura parcial; nunca viram zero ou voto de candidato.

## Execução

Requer Python 3.11+ e apenas a biblioteca padrão.

```bash
cd eleicoes2026
python -m unittest discover -s tests -v
python coletor_tse.py --uf ES --output data/ufs/ES.json
python agregador.py
```

O arquivo `data/auditoria17h_2026_2t.json` contém **somente os municípios e a cobertura consolidada**, consumidos pelo Wix. Os `data/ufs/<UF>.json` contêm resumos por seção sem informações pessoais.

## Automação

`/.github/workflows/auditoria-eleitoral-2026.yml` roda diariamente, às 10h17 UTC (07h17 de Brasília), além de permitir execução manual. Primeiro verifica se o pacote do ES existe, para evitar múltiplas requisições 404 ao TSE. Se estiver disponível, processa UFs com paralelismo limitado, agrega e publica apenas os JSONs. Reprocessamentos já consolidados são ignorados para poupar o serviço do TSE. A conta GitHub Action precisa de permissão `contents: write` para publicar resultados.

## Wix

Site publicado: https://headless-ppsokdlhoxp-gustavohks8-140b.wix-site-host.com/#auditoriaAuto

O frontend acessa automaticamente `eleicoes2026/data/auditoria17h_2026_2t.json` no repositório público, mantém filtros geográficos e exibe o indicador de cobertura. Quando o TSE não disponibilizou os arquivos, mostra **pendência**, não um total nacional de zero.

## Limitações e validação futura

A contagem de log exige reconciliação com comparecimento/boletins oficiais e inspeção de registros faltantes antes de ser considerada completa. A extensão do horário até 19h não deve ser inferida apenas pela existência de eventos pós-17h. Para apurar prejuízo individual é necessário material adicional (ata de seção, controle de filas e provas de tentativa de voto). A coleta pública não armazena identificação de eleitor nem voto escolhido.
