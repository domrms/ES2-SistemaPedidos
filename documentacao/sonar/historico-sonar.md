# Histórico de análises do SonarQube

Este documento registra a evolução da qualidade do projeto durante as análises realizadas em **26 e 27 de junho de 2026**.

Os valores abaixo foram transcritos dos painéis preservados nas capturas de tela. As imagens continuam disponíveis em cada rodada para consulta e auditoria.

## Resultado alcançado

Entre a primeira e a última análise, o projeto evoluiu de:

- **Quality Gate reprovado** para **aprovado**;
- **24 problemas abertos** para **nenhum problema aberto**;
- **40,5% de cobertura** para **95,5%**;
- **1 problema de segurança** para **nenhum**;
- **1 hotspot de segurança pendente** para **nenhum**;
- problemas de confiabilidade e manutenibilidade para **zero**;
- duplicação final de **0,0%**.

> O Quality Gate representa o conjunto de critérios mínimos configurados no SonarQube. Uma análise pode ter boas notas individuais e ainda ser reprovada se ao menos uma condição obrigatória não for atendida.

## Evolução consolidada

| Data e rodada | Quality Gate | Problemas abertos | Cobertura | Duplicação | Segurança | Hotspots | Confiabilidade | Manutenibilidade |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 26/06 — análise inicial | Reprovado | 24 | 40,5% | 0,0% | 1 — nota C | 1 — nota E | 5 — nota A | 20 — nota A |
| 27/06 — rodada 1 | Reprovado | 27 | 62,1% | 0,0% | 1 — nota C | 1 — nota E | 3 — nota A | 23 — nota A |
| 27/06 — rodada 2 | Reprovado | 31 | 56,3% | 0,0% | 0 — nota A | 1 — nota E | 4 — nota A | 27 — nota A |
| 27/06 — rodada 3 | Reprovado | 9 | 53,9% | 0,0% | 0 — nota A | 1 — nota E | 0 — nota A | 9 — nota A |
| 27/06 — rodada 4 | Reprovado | 4 | 53,9% | 3,9% | 0 — nota A | 1 — nota E | 0 — nota A | 4 — nota A |
| 27/06 — rodada 5 | Reprovado | 0 | 95,5% | 3,9% | 0 — nota A | 0 — nota A | 0 — nota A | 0 — nota A |
| 27/06 — rodada 6 | **Aprovado** | **0** | **95,5%** | **0,0%** | **0 — nota A** | **0 — nota A** | **0 — nota A** | **0 — nota A** |

## Linha do tempo detalhada

### 26/06/2026 — análise inicial

Primeiro diagnóstico registrado. O Quality Gate foi reprovado, com 24 problemas abertos, cobertura de 40,5%, um problema de segurança e um hotspot ainda não revisado.

<details>
<summary>Exibir capturas da análise inicial</summary>

#### Visão geral e segurança

![SonarQube na análise inicial: Quality Gate reprovado, 24 problemas, cobertura de 40,5% e nota C em segurança](image.png)

#### Hotspots e confiabilidade

![SonarQube na análise inicial: um hotspot pendente com nota E e cinco problemas de confiabilidade](image-1.png)

#### Manutenibilidade

![SonarQube na análise inicial: nota A e 20 problemas de manutenibilidade](image-2.png)

</details>

### 27/06/2026 — rodada 1

A cobertura avançou para 62,1%. Permaneceram um problema de segurança e um hotspot pendente. O total chegou a 27 problemas abertos.

<details>
<summary>Exibir capturas da rodada 1</summary>

#### Visão geral e segurança

![SonarQube na rodada 1: Quality Gate reprovado, 27 problemas e cobertura de 62,1%](image-3.png)

#### Hotspots e confiabilidade

![SonarQube na rodada 1: um hotspot pendente e três problemas de confiabilidade](image-4.png)

#### Manutenibilidade

![SonarQube na rodada 1: nota A e 23 problemas de manutenibilidade](image-5.png)

</details>

### 27/06/2026 — rodada 2

O problema de segurança foi resolvido, elevando a nota de segurança para A. O hotspot continuou pendente e houve aumento temporário dos problemas de confiabilidade e manutenibilidade.

<details>
<summary>Exibir capturas da rodada 2</summary>

#### Visão geral e segurança

![SonarQube na rodada 2: 31 problemas, cobertura de 56,3% e nenhum problema de segurança](image-6.png)

#### Hotspots e confiabilidade

![SonarQube na rodada 2: um hotspot pendente e quatro problemas de confiabilidade](image-7.png)

#### Manutenibilidade

![SonarQube na rodada 2: nota A e 27 problemas de manutenibilidade](image-8.png)

</details>

### 27/06/2026 — rodada 3

Houve uma redução expressiva dos problemas: confiabilidade chegou a zero e manutenibilidade caiu para nove. O Quality Gate ainda falhou em duas condições.

<details>
<summary>Exibir capturas da rodada 3</summary>

#### Visão geral e segurança

![SonarQube na rodada 3: Quality Gate reprovado em duas condições, nove problemas e cobertura de 53,9%](image-9.png)

#### Hotspots e confiabilidade

![SonarQube na rodada 3: um hotspot pendente e nenhum problema de confiabilidade](image-10.png)

#### Manutenibilidade

![SonarQube na rodada 3: nota A e nove problemas de manutenibilidade](image-11.png)

</details>

### 27/06/2026 — rodada 4

Os problemas abertos caíram para quatro, todos os indicadores de segurança e confiabilidade permaneceram sem problemas, e a manutenibilidade chegou a quatro. Nesta rodada surgiu duplicação de 3,9%, e as condições específicas de código novo também ficaram visíveis.

<details>
<summary>Exibir capturas da rodada 4</summary>

#### Visão geral e segurança

![SonarQube na rodada 4: quatro problemas, cobertura de 53,9%, duplicação de 3,9% e segurança A](image-12.png)

#### Hotspots e confiabilidade

![SonarQube na rodada 4: um hotspot registrado e nenhum problema de confiabilidade](image-13.png)

#### Manutenibilidade

![SonarQube na rodada 4: nota A e quatro problemas de manutenibilidade](image-14.png)

#### Condições do código novo

![SonarQube na rodada 4: código novo reprovado por cobertura, duplicação e hotspot não revisado](image-15.png)

Na visão de código novo, as três condições reprovadas eram:

- cobertura de **50,68%**, abaixo dos **80%** exigidos;
- duplicação de **5,23%**, acima dos **3%** permitidos;
- **0% dos hotspots revisados**, abaixo dos **100%** exigidos.

</details>

### 27/06/2026 — rodada 5

Todos os problemas e hotspots foram resolvidos, e a cobertura chegou a 95,5%. Restou uma única condição reprovada: a duplicação de 3,9%.

<details>
<summary>Exibir capturas da rodada 5</summary>

#### Visão geral e segurança

![SonarQube na rodada 5: nenhum problema, cobertura de 95,5% e duplicação de 3,9%](image-16.png)

#### Hotspots e confiabilidade

![SonarQube na rodada 5: nenhum hotspot e nenhum problema de confiabilidade, ambos com nota A](image-17.png)

#### Manutenibilidade

![SonarQube na rodada 5: nenhum problema de manutenibilidade e nota A](image-18.png)

</details>

### 27/06/2026 — rodada 6 — resultado final

Após a remoção da duplicação restante, todas as condições foram atendidas e o **Quality Gate foi aprovado**.

<details open>
<summary>Exibir capturas do resultado final</summary>

#### Visão geral e segurança

![Resultado final do SonarQube: Quality Gate aprovado, nenhum problema, nenhuma duplicação e cobertura de 95,5%](image-19.png)

#### Hotspots e confiabilidade

![Resultado final do SonarQube: nenhum hotspot e nenhum problema de confiabilidade, ambos com nota A](image-20.png)

#### Manutenibilidade

![Resultado final do SonarQube: nenhum problema de manutenibilidade e nota A](image-21.png)

</details>

## Leitura do resultado final

O último painel registra:

| Indicador | Resultado final |
|---|---:|
| Quality Gate | **Aprovado** |
| Problemas abertos | **0** |
| Cobertura | **95,5%** |
| Duplicação | **0,0%** |
| Problemas de segurança | **0 — nota A** |
| Hotspots de segurança | **0 — nota A** |
| Problemas de confiabilidade | **0 — nota A** |
| Problemas de manutenibilidade | **0 — nota A** |

As capturas documentam o estado do projeto em cada análise. Os valores correntes podem mudar à medida que o código e as regras do Quality Gate evoluírem.
