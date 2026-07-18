---
title: "Anteprojeto de Pesquisa de Mestrado"
---

# Modelo Computacional para Representação e Análise da Evolução Arquitetural em Sistemas Distribuídos

**Candidato(a):** [A PREENCHER]
**Programa de Pós-Graduação:** [A PREENCHER]
**Linha de Pesquisa:** [A PREENCHER]
**Orientador(a) pretendido(a):** [A PREENCHER]
**Área de Concentração:** Engenharia de Software / Sistemas Distribuídos

---

## Resumo

Sistemas distribuídos modernos, compostos por milhares de microsserviços, evoluem de forma contínua e frequentemente imprevisível. Ferramentas de observabilidade atuais (ex.: OpenTelemetry, Dynatrace, Datadog) coletam grande volume de dados de execução, mas não representam formalmente o **comportamento arquitetural** do sistema ao longo do tempo. Este anteprojeto propõe investigar a existência de um modelo computacional capaz de representar, analisar e apoiar decisões sobre a evolução arquitetural de sistemas distribuídos, tratando a arquitetura como uma estrutura de dados passível de análise algorítmica, e não apenas como um artefato de visualização. Palavras-chave: arquitetura de software; microsserviços; modelo computacional; acoplamento; evolução arquitetural.

---

## 1. Introdução

A adoção de arquiteturas de microsserviços permitiu escalabilidade e autonomia de times, mas introduziu um novo tipo de complexidade: a arquitetura de um sistema com milhares de serviços deixa de ser algo que uma pessoa consegue reter mentalmente, e passa a ser um objeto que evolui de forma parcialmente invisível aos próprios responsáveis por ele. Este trabalho parte da constatação de que existem hoje ferramentas maduras de observabilidade, mas nenhuma delas oferece uma **representação formal do comportamento arquitetural** que permita responder, de forma sistemática, a perguntas como "este serviço está mais crítico que há seis meses?" ou "qual será o impacto de dividir este serviço em dois?".

## 2. Problema de Pesquisa

Formalmente, o problema de pesquisa deste trabalho é:

> **Qual é o modelo computacional mais adequado para representar, analisar e compreender arquiteturas distribuídas em tempo de execução?**

Este problema é motivado por perguntas recorrentes na prática de arquitetura de software que hoje não têm resposta sistemática, entre elas: identificação de serviços críticos, detecção de crescimento de acoplamento, previsão de impacto de mudanças antes do deploy, e diferenciação entre dependências declaradas e dependências efetivamente exercidas em produção.

## 3. Justificativa

A justificativa deste trabalho apoia-se em três pontos:

1. **Relevância prática:** empresas que operam sistemas de grande escala (milhares de microsserviços) enfrentam decisões arquiteturais de alto risco sem suporte sistemático, dependendo primariamente da experiência tácita de arquitetos seniores.
2. **Lacuna aparente na literatura:** levantamento preliminar (não sistemático) identificou trabalhos relevantes sobre grafos de dependência, métricas de acoplamento, dívida técnica arquitetural e análise de causa raiz — mas de forma fragmentada, sem uma síntese unificadora que trate a arquitetura, de forma explícita, como uma estrutura de dados sujeita a algoritmos formais de simulação e predição (ver Seção 5).
3. **Oportunidade metodológica:** a maturidade atual de infraestrutura de observabilidade (tracing distribuído, telemetria padronizada) torna viável, pela primeira vez em escala, coletar os dados necessários para testar empiricamente um modelo dessa natureza.

## 4. Objetivos

### 4.1 Objetivo Geral

Investigar a viabilidade e propor um modelo computacional capaz de representar, analisar e apoiar a compreensão da arquitetura dinâmica de sistemas distribuídos em tempo de execução.

### 4.2 Objetivos Específicos

1. Realizar revisão sistemática da literatura para determinar se há, de fato, uma lacuna quanto a modelos formais de representação de arquitetura dinâmica (ver Seção 6.1).
2. Caracterizar formalmente o acoplamento em tempo de execução entre componentes distribuídos.
3. Modelar a propagação de falhas sobre a estrutura de dependências.
4. Propor e avaliar um mecanismo de simulação de impacto de mudanças arquiteturais previamente à sua implantação.
5. Avaliar o modelo proposto em um estudo de caso com dados reais ou realistas de um sistema distribuído em produção.

## 5. Referencial Teórico e Trabalhos Relacionados (síntese preliminar)

Um levantamento preliminar identificou seis linhas de trabalho relacionadas, que serão aprofundadas na revisão sistemática (Seção 6.1):

- **Representação de arquitetura como grafo** (GAIDELS; KIRIKOVA, 2020; grafos de dependência de serviço).
- **Métricas de acoplamento** (ZHONG et al., 2023; PEREPLETCHIKOV et al., 2007).
- **Monitoramento da evolução do acoplamento ao longo do tempo**, incluindo trabalho brasileiro que analisa acoplamento como série temporal (Journal of the Brazilian Computer Society, 2021) — trabalho especialmente próximo da proposta e que deverá ser discutido em detalhe.
- **Erosão arquitetural e dívida técnica arquitetural** (LI et al., 2022; MARTINI; BOSCH; CHAUDRON, 2014; VERDECCHIA; MALAVOLTA; LAGO, 2018).
- **Análise de causa raiz e propagação de falhas (RCA/AIOps)** (SOLDANI; BROGI, 2021; WANG; QI, 2024).
- **Reconstrução arquitetural e análise de impacto de mudança** (arXiv:2501.11778, 2025; BOHNER; ARNOLD, 1996; MURPHY; NOTKIN; SULLIVAN, 2001).

A lista completa, com anotações de relevância, está no documento complementar "Bibliografia Recomendada" que acompanha este anteprojeto.

## 6. Metodologia

A pesquisa será conduzida em três fases sequenciais:

### 6.1 Fase 1 — Revisão Sistemática da Literatura

Execução de revisão sistemática seguindo protocolo formal (strings de busca, critérios de inclusão/exclusão, bases: IEEE Xplore, ACM Digital Library, Scopus, Web of Science), com o objetivo explícito de tentar **refutar** a hipótese de que existe uma lacuna. Critério de sucesso: apenas se, após análise de volume substancial de literatura (meta de referência: ~50 artigos), a conclusão permanecer de que não existe um modelo formal equivalente, a pesquisa prossegue para a Fase 2.

### 6.2 Fase 2 — Formalização do Modelo

Definição formal da representação computacional (estrutura de dados, propriedades, invariantes) e dos algoritmos associados (propagação de falha, impacto de mudança, medição de acoplamento ao longo do tempo).

### 6.3 Fase 3 — Avaliação Empírica

Aplicação do modelo a dado(s) real(is) ou realista(s) de sistema(s) distribuído(s), comparando resultados com abordagens existentes (Seção 5) e discutindo limitações.

## 7. Cronograma (preliminar)

| Atividade | Meses 1–3 | Meses 4–6 | Meses 7–9 | Meses 10–12 | Meses 13–18 | Meses 19–24 |
|---|---|---|---|---|---|---|
| Revisão sistemática da literatura | X | X | | | | |
| Delimitação formal do problema e hipóteses | | X | X | | | |
| Formalização do modelo computacional | | | X | X | | |
| Implementação de protótipo | | | | X | X | |
| Avaliação empírica / estudo de caso | | | | | X | X |
| Redação da dissertação | | | | | X | X |
| Qualificação | | | X | | | |
| Defesa final | | | | | | X |

## 8. Resultados Esperados

Dependendo do resultado da Fase 1, espera-se como contribuição final um dos seguintes artefatos (a definir formalmente após a revisão sistemática): um modelo computacional formal, um método de análise arquitetural baseado em dados de execução, ou um conjunto de algoritmos fundamentados nesse modelo (propagação de falha, impacto de mudança, medição de acoplamento temporal).

## Referências (formato ABNT, preliminar)

BESKER, T.; MARTINI, A.; BOSCH, J. Managing architectural technical debt: A unified model and systematic literature review. **Journal of Systems and Software**, v. 135, p. 1–16, 2018.

BOHNER, S. A.; ARNOLD, R. S. **Software Change Impact Analysis**. IEEE Computer Society Press, 1996.

GAIDELS, E.; KIRIKOVA, M. Service Dependency Graph Analysis in Microservice Architecture. In: **Perspectives in Business Informatics Research (BIR 2020)**. Springer, 2020.

LI, R.; LIANG, P.; SOLIMAN, M.; AVGERIOU, P. Understanding software architecture erosion: A systematic mapping study. **Journal of Software: Evolution and Process**, v. 34, n. 3, e2423, 2022.

MARTINI, A.; BOSCH, J.; CHAUDRON, M. Architecture technical debt: Understanding causes and a qualitative model. In: **EUROMICRO Conference on Software Engineering and Advanced Applications**, IEEE, 2014.

MURPHY, G.; NOTKIN, D.; SULLIVAN, K. Software reflexion models: bridging the gap between design and implementation. **IEEE Transactions on Software Engineering**, 2001.

PEREPLETCHIKOV, M.; RYAN, C.; FRAMPTON, K.; TARI, Z. Coupling metrics for predicting maintainability in service-oriented designs. In: **ASWEC'07**, IEEE, 2007.

SOLDANI, J.; BROGI, A. Anomaly Detection and Failure Root Cause Analysis in (Micro)Service-Based Cloud Applications: A Survey. **Journal of Systems and Software**, v. 178, 2021.

VERDECCHIA, R.; MALAVOLTA, I.; LAGO, P. Architectural technical debt identification: The research landscape. In: **International Conference on Technical Debt**, p. 11–20, 2018.

WANG, T.; QI, G. A Comprehensive Survey on Root Cause Analysis in (Micro)Services: Methodologies, Challenges, and Trends. arXiv:2408.00803, 2024.

ZHONG, C.; ZHANG, H.; LI, C. et al. On measuring coupling between microservices. **Journal of Systems and Software**, v. 200, 2023.

*(Lista completa e anotada de referências: ver documento "Bibliografia Recomendada".)*

---

> **Nota sobre este protótipo:** este documento é um **modelo de estrutura**, não um anteprojeto pronto para submissão. Os objetivos específicos, o referencial teórico e o cronograma precisam ser ajustados após a Fase 1 (revisão sistemática). Serve como referência de formato — verifique também as normas específicas de formatação exigidas pelo seu programa de pós-graduação (ABNT, template institucional, número de páginas etc.), que podem diferir do aqui apresentado.
