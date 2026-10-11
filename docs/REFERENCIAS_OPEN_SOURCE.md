# Catálogo de tecnologias open-source — referência de arquitetura

Este documento registra bibliotecas candidatas para uso **seletivo**, conforme necessidade, benchmarks e revisão de licença. **Não significa que estejam instaladas ou integradas.** Evitar incluir dependências sem caso de uso confirmado.

| Tecnologia | Repositório oficial | Uso indicado |
|---|---|---|
| PaddleOCR | https://github.com/PaddlePaddle/PaddleOCR | OCR de documentos, formulários, placas (avaliar precisão com amostras reais) |
| Tesseract OCR | https://github.com/tesseract-ocr/tesseract | OCR leve como alternativa ou baseline |
| whisper.cpp | https://github.com/ggml-org/whisper.cpp | Transcrição de áudio local, quando autorizada |
| html5-qrcode | https://github.com/mebjas/html5-qrcode | Leitura de QR codes e códigos de barras em páginas web |
| WebLLM | https://github.com/mlc-ai/web-llm | Inferência de LLM no navegador em dispositivos compatíveis, opcional |
| MapLibre GL JS | https://github.com/maplibre/maplibre-gl-js | Mapas vetoriais e geovisualizações; provedor de tiles separado |
| Univer | https://github.com/dream-num/univer | Planilhas editáveis no navegador; validar licenças de módulos |
| Frappe Gantt | https://github.com/frappe/gantt | Cronogramas interativos e planejamento |
| rclone | https://github.com/rclone/rclone | Sincronização e cópia entre armazenamento e nuvens |
| OpenCV | https://github.com/opencv/opencv | Processamento de imagens e vídeo; pré-processamento para OCR |

## Padrões de integração
1. Adotar por demanda funcional real, evitando novas dependências no frontend/backend sem necessidade.
2. Preferir integração modular por serviço/adaptador, configurada por feature flag; não interromper produção.
3. Registrar versão, licença, SBOM/dependências, riscos de segurança e requisitos de hardware (CPU/GPU, RAM).
4. Validar desempenho, taxa de acerto e regressões com amostras reais antes de promover para produção.
5. Para CFTV/OCR: usar OpenCV no recorte/qualidade, PaddleOCR como candidato principal, Tesseract como comparação; não tratar hipótese de placa como identificação confirmada.
6. Dados pessoais, imagens, áudios e placas: acesso restrito, retenção mínima, trilha de auditoria e conformidade LGPD.
7. Mapas: verificar licença/termos do provedor de tiles e custos; backups rclone com criptografia, monitoramento e testes de restauração.

## Aplicações no ecossistema
- CFTV JOMAD: OpenCV + PaddleOCR/Tesseract para placas, confirmação por cruzamento com Autotrac.
- NFes Hub / Oficina Hub: PaddleOCR/Tesseract em PDFs digitalizados e OS; html5-qrcode para identificação em campo.
- Fuel Hub / AutoLog: html5-qrcode; MapLibre em roteiros/locais; OCR somente se comprovado necessário.
- Data Hub / Pedágio Hub / Fleet Insight: MapLibre; Univer para conciliações; Frappe Gantt para cronogramas.
- JOMAD Drive / Center: rclone no backend, logs e rotinas de backup; Univer para edição de planilhas, se necessário.
- GranaOK / Controle Financeiro Pessoal: Univer para grade editável se útil; OCR de comprovantes facultativo; WebLLM apenas se justificar IA local.
- LifeOS: Frappe Gantt para planejamento; WebLLM e whisper.cpp opcionais conforme caso de uso.

**Status:** referências técnicas catalogadas; integração de runtime pendente de implementação e validação por projeto.
