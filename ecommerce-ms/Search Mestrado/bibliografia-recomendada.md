# Bibliografia Recomendada — Base de Sustentação Científica

Esta lista foi levantada por busca direta (não é reconstrução de memória) e organizada em **6 clusters temáticos**, cada um mapeado a uma seção específica do documento de base científica. Ela é um **ponto de partida** para a revisão sistemática descrita na Seção 9 do documento principal — não a substitui. São ~20 referências; a meta declarada no documento é revisar ao menos ~50 trabalhos.

Cada referência traz uma nota curta ligando o trabalho à sua pergunta de pesquisa.

---

## Cluster A — Representação de Arquitetura como Grafo (base para Seção 4 e 7)

1. **GAIDELS, E.; KIRIKOVA, M.** Service Dependency Graph Analysis in Microservice Architecture. In: *Perspectives in Business Informatics Research (BIR 2020)*, LNBIP vol. 398, Springer, 2020. DOI: 10.1007/978-3-030-61140-8_9
 *Nota: aplica algoritmos de grafo (centralidade, comunidades) para avaliar qualidade arquitetural — proximidade direta com sua proposta de "arquitetura como estrutura de dados".*

2. **NURMELA, T.; NEVAVUORI, P.; RAHMAN, I.** Qualitative evaluation of dependency graph representativeness. *CEUR Workshop Proceedings*, vol. 2520, p. 37–44, 2019.
 *Nota: discute o quão fiel um grafo de dependências é em relação ao comportamento real — relevante para sua distinção entre dependência "declarada" vs. "exercida".*

3. **TIWARI, A.** Unveiling Graph Structures in Microservices: Service Dependency Graph, Call Graph, and Causal Graph. 2024.
 *Nota: fonte expositiva (não peer-reviewed) que consolida os três tipos de grafo usados na literatura — útil como mapa inicial de conceitos antes de ir às fontes primárias.*

4. IEEE — Using Service Dependency Graph to Analyze and Test Microservices (GMAT/GSMART). *IEEE Conference Publication*, 2018.
 *Nota: geração automática de grafo de dependência de serviços a partir de execução — precursor direto da "Etapa 2: Modelo Computacional" do seu pipeline.*

---

## Cluster B — Acoplamento e Métricas de Qualidade Arquitetural (base para Seção 7.2, item 1)

5. **ZHONG, C.; ZHANG, H.; LI, C. et al.** On measuring coupling between microservices. *Journal of Systems and Software*, vol. 200, 2023.
 *Nota: propõe o "Microservice Coupling Index" — métrica comparável de acoplamento; referência direta para sua pergunta "onde o acoplamento está crescendo?".*

6. **PEREPLETCHIKOV, M.; RYAN, C.; FRAMPTON, K.; TARI, Z.** Coupling metrics for predicting maintainability in service-oriented designs. In: *ASWEC'07*, IEEE, 2007.
 *Nota: referência clássica e fundacional de métricas de acoplamento em SOA, ainda citada por trabalhos recentes.*

7. **[Journal of the Brazilian Computer Society, 2021]** A method for monitoring the coupling evolution of microservice-based architectures. DOI: 10.1186/s13173-021-00120-y
 *Nota: ⭐ referência crítica — analisa acoplamento como série temporal (não snapshot), detecta tendência de degradação, e é brasileira. É praticamente o mesmo objetivo da sua Seção 11.4 ("complexidade como série ao longo do tempo"). Precisa entrar na sua discussão de trabalhos relacionados obrigatoriamente.*

---

## Cluster C — Erosão Arquitetural e Dívida Técnica Arquitetural (base para Seção 3, itens 8–10)

8. **LI, R.; LIANG, P.; SOLIMAN, M.; AVGERIOU, P.** Understanding software architecture erosion: A systematic mapping study. *Journal of Software: Evolution and Process*, 34(3), e2423, 2022.
 *Nota: mapeamento sistemático — modelo de referência para como você mesmo deve conduzir sua própria revisão (Seção 9).*

9. Understanding Architecture Erosion: The Practitioners' Perspective. arXiv:2103.11392.
 *Nota: estudo qualitativo com praticantes sobre causas e sintomas de erosão — dá vocabulário para sua Seção 3 ("dívida arquitetural").*

10. **MARTINI, A.; BOSCH, J.; CHAUDRON, M.** Architecture technical debt: Understanding causes and a qualitative model. In: *EUROMICRO SEAA*, IEEE, 2014.
 *Nota: um dos trabalhos fundadores do conceito de dívida técnica arquitetural.*

11. **VERDECCHIA, R.; MALAVOLTA, I.; LAGO, P.** Architectural technical debt identification: The research landscape. In: *International Conference on Technical Debt*, 2018.
 *Nota: mapeia o "estado da arte" de identificação de dívida arquitetural — comparável diretamente ao exercício que você vai fazer na Seção 9.*

12. **BESKER, T.; MARTINI, A.; BOSCH, J.** Managing architectural technical debt: A unified model and systematic literature review. *Journal of Systems and Software*, 135, p. 1–16, 2018.
 *Nota: revisão sistemática com modelo unificado — referência de método e de conteúdo.*

---

## Cluster D — Análise de Causa Raiz e Propagação de Falhas / AIOps (base para Seção 7.2, item 3)

13. **SOLDANI, J.; BROGI, A.** Anomaly Detection and Failure Root Cause Analysis in (Micro)Service-Based Cloud Applications: A Survey. *Journal of Systems and Software*, 178, 2021. arXiv:2105.12378
 *Nota: survey de referência para propagação de falhas em microsserviços — leitura obrigatória antes de formalizar seu "algoritmo de propagação de falha".*

14. **WANG, T.; QI, G.** A Comprehensive Survey on Root Cause Analysis in (Micro)Services: Methodologies, Challenges, and Trends. arXiv:2408.00803, 2024.
 *Nota: survey mais recente (2024), cobre inclusive abordagens com LLM — bom para discutir a Etapa 4 (LLM) do seu pipeline.*

15. **BRANDÓN, Á. et al.** Graph-based root cause analysis for service-oriented and microservice architectures. *Journal of Systems and Software*, 2022 (citado via jisem-journal.com).
 *Nota: constrói grafos de dependência e navega neles para localizar causa raiz — mais um antecedente direto do seu modelo.*

---

## Cluster E — Reconstrução Arquitetural e Análise de Impacto de Mudança (base para Seção 7.2, item 4 e Seção 11)

16. Towards Infrastructure For Change Impact Analysis in Microservices: Early Findings and Prototyping. arXiv:2501.11778, 2025.
 *Nota: ⭐ referência muito próxima da sua proposta — usa reconstrução arquitetural incremental para prever impacto de mudança antes do deploy, exatamente sua pergunta #5 (Seção 3). Comparar diretamente com seu "algoritmo de impacto de mudança".*

17. **BOHNER, S. A.; ARNOLD, R. S.** *Software Change Impact Analysis*. IEEE Computer Society Press, 1996.
 *Nota: referência fundacional (clássica, pré-microsserviços) do campo de análise de impacto de mudança — necessária para mostrar a linhagem histórica do problema.*

18. **MURPHY, G.; NOTKIN, D.; SULLIVAN, K.** Software reflexion models: bridging the gap between design and implementation. *IEEE Transactions on Software Engineering*, 2001.
 *Nota: trabalho clássico sobre comparar arquitetura pretendida (declarada) vs. arquitetura real (extraída) — fundamento teórico direto da sua distinção "declarado vs. exercido".*

---

## Cluster F — Detecção de Anti-Padrões Arquiteturais Dinâmicos (complementar à Seção 7)

19. **ABDELFATTAH, A. S.; CERNÝ, T. et al.** Automatic Anti-Pattern Detection in Microservice Architectures Based on Distributed Tracing.
 *Nota: detecção de anti-padrões (mega-service, bottleneck-service, nano-service etc.) via tracing distribuído — relevante para os algoritmos de "gargalo" mencionados na sua Seção 3.*

20. Using dependency graph and graph theory concepts to identify anti-patterns in a microservices system: A tool-based approach (mapeamento sistemático, 31 artigos analisados).
 *Nota: outro exemplo de mapeamento sistemático de escopo menor — útil como referência metodológica para dimensionar a sua própria revisão.*

---

## Como usar esta lista

1. **Não trate como bibliografia final.** É um ponto de partida real (20 referências, buscadas agora), não o resultado da revisão sistemática da Seção 9 — essa ainda precisa ser feita com rigor (strings de busca formais, critérios de inclusão/exclusão, bases como IEEE Xplore, ACM DL, Scopus, Web of Science).
2. **O Cluster B, referência 7, e o Cluster E, referência 16, são os mais próximos da sua ideia central.** Eles devem ser lidos primeiro e discutidos com cuidado na seção de trabalhos relacionados — é ali que normalmente uma banca pergunta "e por que isso não é apenas o que o Zhong et al. (2023) [ou o trabalho brasileiro do JBCS] já fez?".
3. Sugestão de próximo passo: para cada cluster, buscar os artigos que **citam** essas referências (forward citation search, via Google Scholar/Semantic Scholar) — isso normalmente revela os 2–3 anos mais recentes de evolução do tema.
