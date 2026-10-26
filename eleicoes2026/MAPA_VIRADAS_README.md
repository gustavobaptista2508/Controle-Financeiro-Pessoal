# Mapa das Viradas Municipais — Eleições 2026

Módulo do **BI Eleições 2026** já hospedado no Wix. Mostra municípios brasileiros em que mudou o candidato presidencial mais votado entre 4 e 25 de outubro de 2026, com mapa de pontos, tabela e filtros. Não infere a escolha de eleitores individuais.

## Situação e resultados conferidos (26/10/2026)

O processamento gerou **5.567 municípios comparáveis** e encontrou **212 viradas no critério de liderança geral**: **211 para Lula e 1 para Flávio**. Por região: Sudeste 120, Sul 40, Norte 21, Centro-Oeste 21 e Nordeste 10. O número não é hardcoded no frontend; vem do arquivo consolidado e poderá mudar caso o TSE atualize a totalização.

## Fonte dos dados e proveniência

- **Votos do 1º turno:** arquivo oficial do TSE `https://cdn.tse.jus.br/estatistica/sead/odsele/votacao_candidato_munzona/votacao_candidato_munzona_2026.zip` (CSV nacional, ISO-8859-1, separado por ponto e vírgula).
- **Votos do 2º turno:** o mesmo ZIP oficial quando contiver dados de ambos os turnos. Se ainda trouxer somente o primeiro, a automação usa a **republicação municipal derivada do TSE pelo NOPolitica**: `https://nopolitica.com.br/dados/2026/presidente-2-turno-municipios.csv`. O script verifica se os totais municipais se aproximam, sem exceder, os números oficiais nacionais publicados pelo TSE (Lula 64.256.232; Flávio 59.826.041); não publica dados quando a validação falha. O JSON registra a fonte realmente utilizada.
- **Georreferenciamento:** correspondência dos códigos TSE ↔ IBGE (projeto `betafcc/Municipios-Brasileiros-TSE`) e coordenadas do projeto `kelvins/municipios-brasileiros`.
- **Mapa-base:** GeoJSON estadual do projeto `henriquemalvar/br-geojson`, utilizado também pelo BI original.

O material do NOPolitica é compilação de dados públicos do TSE e requer atribuição; a interface identifica explicitamente a republicação quando utilizada.

## Critérios

1. **Liderança geral:** seleciona o candidato mais votado de todos os concorrentes no 1º turno; compara com o mais votado entre Lula e Flávio no 2º turno. Cidade mudou de líder quando os números de candidato são diferentes.
2. **Lula × Flávio:** seleciona o mais votado exclusivamente entre os candidatos números 13 e 22 em cada turno, independentemente da votação de terceiros no 1º.
3. **Variação da margem:** `(Lula 2º − Flávio 2º) − (Lula 1º − Flávio 1º)`, em votos absolutos.
4. Empates e municípios sem ambos os turnos são excluídos das viradas. Votos no exterior (`ZZ`) não são representados geograficamente.
5. Os pontos marcam coordenadas aproximadas de sedes municipais, não a área territorial de votação.

## Arquitetura

- `eleicoes2026/gerar_mapa_viradas.py`: coleta e agrega os CSVs por município, turno e candidato; verifica cobertura e compara os totais do segundo turno com os nacionais oficiais.
- `eleicoes2026/tests/test_mapa.py`: testes unitários do parser e da definição de virada.
- `.github/workflows/mapa-viradas-2026.yml`: rotina agendada diariamente e acionável pelo GitHub Actions. Falhas de disponibilidade/validação interrompem a publicação; nada é fabricado.
- `eleicoes2026/data/viradas_municipios_2026.json`: arquivo final consumido pelo frontend via GitHub Raw (com CDN alternativa).
- Wix: `mapa-viradas.js`, `mapa-viradas.css` e seção `#mapaViradas2026` embutida no `index.html` do BI já existente.

## Restrições

- O mapa não permite inferir quem mudou seu voto ou em quem votaram as pessoas após as 17h.
- A correlação geográfica não é prova de irregularidade eleitoral.
- Quando a fonte do 2º turno for uma republicação, sua identificação fica visível na interface e no JSON. O arquivo do TSE é priorizado quando contemplar os dois turnos.
- A contagem nacional da foto do Instagram não foi tomada como entrada: foi recalculada e coincidiu na execução desta data.
