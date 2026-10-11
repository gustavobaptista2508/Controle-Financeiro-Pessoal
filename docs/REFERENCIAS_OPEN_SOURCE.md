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

**Status geral: 20 tecnologias documentadas; nenhuma implementação adicional está implícita nesta atualização do catálogo.**
