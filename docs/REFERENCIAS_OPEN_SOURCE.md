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

## Complemento — tecnologias 11 a 20 (11/10/2026)

As ferramentas abaixo são **candidatas à adoção**, não dependências instaladas.

| Tecnologia | Repositório oficial | Uso indicado |
|---|---|---|
| Haystack | https://github.com/deepset-ai/haystack | RAG/busca semântica em documentos internos, com autorização e indexação segura |
| Sherpa-ONNX | https://github.com/k2-fsa/sherpa-onnx | Reconhecimento e síntese de voz offline, conforme modelos/dispositivos compatíveis |
| Mediabunny | https://github.com/Vanilagy/mediabunny | Processamento/transcodificação de mídia no navegador; validar suporte de codecs e plataforma |
| React PDF Highlighter | https://github.com/agentcooper/react-pdf-highlighter | Marcações e comentários sobre PDFs, persistidos no backend com permissões |
| React Grid Layout | https://github.com/react-grid-layout/react-grid-layout | Dashboards responsivos com widgets reposicionáveis |
| Annotorious | https://github.com/annotorious/annotorious | Anotações e áreas de interesse em imagens, como recortes de câmera |
| IfcOpenShell | https://github.com/IfcOpenShell/IfcOpenShell | Leitura/análise de modelos IFC/BIM quando existir projeto de engenharia compatível |
| Gitleaks | https://github.com/gitleaks/gitleaks | Varredura preventiva de senhas, tokens e segredos em código e pipelines CI |
| Glide Data Grid | https://github.com/glideapps/glide-data-grid | Tabelas com muitas linhas e edição de células, conforme requisitos do frontend |
| OpenAPI Generator | https://github.com/OpenAPITools/openapi-generator | Geração de SDKs/clientes/servidores a partir de contratos OpenAPI validados |

### Aplicações preferenciais
- **JOMAD Center e dashboards dos Hubs:** React Grid Layout, Glide Data Grid e Gitleaks nos pipelines de desenvolvimento; OpenAPI Generator se houver especificações OpenAPI mantidas.
- **NFes Hub:** React PDF Highlighter para notas/documentos; Glide Data Grid para listagens; Haystack apenas como consulta contextual com controle de acesso, sem substituir XML/documento oficial.
- **Oficina Hub:** React PDF Highlighter para ordens digitalizadas; Haystack para manuais autorizados; Sherpa-ONNX opcional para registro por voz; Frappe Gantt para planejamento.
- **CFTV JOMAD:** Mediabunny para mídia web quando suportada e Annotorious para revisar recortes/placas; manter pipeline OpenCV + OCR para detecção. Exigir retenção, permissões e auditoria.
- **JOMAD Drive:** Haystack para documentos indexados por ACL, anotações em PDF e rclone para backups; não indexar dados privados sem regras de acesso.
- **Fuel Hub / Data Hub / Pedágio Hub:** Glide Data Grid para tabelas volumosas, React Grid Layout para painéis e OpenAPI Generator onde contratos forem estáveis.
- **GranaOK / Controle Financeiro Pessoal / LifeOS:** dashboards configuráveis, tabelas e eventual consulta de documentos; minimizar coleta e processamento de dados pessoais.
- **Engenharia / obras:** IfcOpenShell só quando houver arquivos IFC/BIM e demanda real; desenhos simples 2D não exigem BIM.

### Controles adicionais antes de integrar
1. Verificar licença **do repositório e dos componentes/modelos/codecs utilizados**, inclusive uso comercial e distribuição.
2. Priorizar **Gitleaks em CI e no pré-commit**; caso detecte segredo exposto, revogar/rotacionar credenciais, não somente apagar do Git.
3. Para Haystack, garantir isolamento por empresa/usuário, ACL no momento da busca, trilhas de auditoria e política de indexação/desindexação.
4. Para voz, mídia e anotações, obter autorização e respeitar LGPD, limites de hardware e formatos suportados.
5. Para Glide Data Grid, React Grid Layout e bibliotecas React, validar compatibilidade com o frontend efetivamente existente; não migrar stack apenas para adotar biblioteca.
6. Gerar SDKs com OpenAPI Generator somente com contratos versionados; revisar diff e testar integração contra homologação.

**Status do lote anterior: 20 tecnologias documentadas; nenhuma implementação adicional está implícita.**


## Lote 3 — 10 bibliotecas adicionais (11/10/2026)

| Tecnologia / Fonte | Finalidade | Aplicações candidatas |
|---|---|---|
| [pdfme](https://github.com/pdfme/pdfme) | Templates e geração programática de PDF | OS de oficina, relatórios de frota, documentos timbrados, comprovantes e GranaOK |
| [GrapesJS](https://github.com/GrapesJS/grapesjs) | Editor visual de páginas e modelos HTML | Templates de comunicação, páginas internas e formulários, com publicação controlada |
| [FullCalendar](https://github.com/fullcalendar/fullcalendar) | Agendas e calendários interativos | Oficina Hub, manutenções preventivas, Center e planejamento de tarefas |
| [Presidio](https://github.com/data-privacy-stack/presidio) | Detecção e anonimização de informações pessoais | Dados de motoristas e colaboradores, relatórios e IA documental; revisar reconhecedores para CPF/CNPJ |
| [Yjs](https://github.com/yjs/yjs) | Sincronização colaborativa/CRDT em tempo real | Documentos e edições colaborativas no JOMAD Drive; autenticação e permissões no servidor |
| [XYFlow](https://github.com/xyflow/xyflow) | Editor visual de nós, conexões e fluxos | Desenho de processos e integrações no Center; execução separada e autorizada |
| [Uppy](https://github.com/transloadit/uppy) | Interface de upload de arquivos, incluindo uploads retomáveis | XML/PDF/ZIP no NFes Hub; fotos de manutenção; arquivos do Drive e AutoLog |
| [Playwright](https://github.com/microsoft/playwright) | Testes end-to-end de interfaces web | Regressão de JOMAD Center, Fuel Hub, NFes Hub, Drive e GranaOK |
| [ReceiptLine](https://github.com/receiptline/receiptline) | Geração de layouts de cupons para impressoras térmicas | Recibos dos postos internos e comprovantes do Fuel Hub; verificar modelo/impressora |
| [Motion Canvas](https://github.com/motion-canvas/motion-canvas) | Animação em vídeo por código | Treinamentos internos, tutoriais e apresentações, não ferramenta essencial de produção |

### Critérios de adoção do lote 3
1. **Licenças:** confirmar versão, licença atual e condições específicas de distribuição comercial de cada biblioteca e dos recursos associados antes de incorporar; não presumir que tudo é livre de restrições.
2. **PDFs e conteúdo editável:** tratar PDFs, HTML e arquivos enviados como entradas não confiáveis; validar esquemas, sanitizar conteúdos e restringir permissões.
3. **Presidio:** a detecção não garante anonimização completa; usar testes com padrões brasileiros (CPF, CNPJ, placas), revisão de falsos negativos e controles LGPD.
4. **Yjs:** sincronização não substitui autorização, isolamento por documento, trilha de alterações e backups.
5. **Uppy:** validar tipo, tamanho, conteúdo, antivírus conforme risco, autenticação e armazenamento seguro; uploads resumíveis exigem backend adequado.
6. **Playwright:** usar preferencialmente em homologação, com credenciais de teste e sem automatizar ações destrutivas em produção.
7. **ReceiptLine:** homologar comandos, largura de papel, codificação e drivers de cada impressora.
8. **XYFlow/GrapesJS:** representações visuais não devem permitir execução de código ou processos administrativos sem validação e autorização.

**Status geral: 30 tecnologias documentadas como referências, sem instalação, integração ou implantação automática.**
