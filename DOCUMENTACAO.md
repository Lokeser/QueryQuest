# QueryQuest — Relatório Técnico Completo

Documentação integral do projeto: da proposta acadêmica à implementação, com o
funcionamento detalhado de cada sistema.

**Autor:** Guilherme Muniz Narciso
**Orientador:** Fábio dos Santos Gonçalves
**Curso:** Sistemas de Informação — Centro Universitário Geraldo Di Biase (UGB/FERP)
**Engine:** Unity 6 (6000.3.10f1) · **Linguagem:** C# · **Banco:** SQLite

---

## Sumário

1. [A proposta acadêmica](#1-a-proposta-acadêmica)
2. [O problema que o jogo ataca](#2-o-problema-que-o-jogo-ataca)
3. [Visão geral do jogo](#3-visão-geral-do-jogo)
4. [Arquitetura do projeto](#4-arquitetura-do-projeto)
5. [O banco de dados](#5-o-banco-de-dados)
6. [O interpretador SQL](#6-o-interpretador-sql)
7. [O sistema de combate](#7-o-sistema-de-combate)
8. [Economia de mana: o retorno graduado](#8-economia-de-mana-o-retorno-graduado)
9. [Posicionamento e distância](#9-posicionamento-e-distância)
10. [Progressão: fragmentos, JOIN e roguelite](#10-progressão-fragmentos-join-e-roguelite)
11. [A interface](#11-a-interface)
12. [Sistemas de apoio](#12-sistemas-de-apoio)
13. [Fluxo completo de uma partida](#13-fluxo-completo-de-uma-partida)
14. [Decisões técnicas e seus porquês](#14-decisões-técnicas-e-seus-porquês)
15. [Validação](#15-validação)
16. [Limitações conhecidas](#16-limitações-conhecidas)

---

## 1. A proposta acadêmica

O QueryQuest é um **jogo educacional de combate por turnos com estrutura de
progressão roguelite**, no qual as consultas SQL escritas pelo jogador
constituem o próprio sistema de magia. Não há botão de "atacar": para lançar um
feitiço, o jogador escreve uma consulta real, que é executada contra um banco
SQLite embarcado, e a magia devolvida pela consulta é a que sai.

### Classificação do trabalho

| Dimensão | Classificação |
|---|---|
| Natureza | Pesquisa aplicada — produz um artefato para um problema concreto |
| Objetivos | Exploratória e descritiva |
| Abordagem | Quali-quantitativa |
| Procedimento | Pesquisa bibliográfica + desenvolvimento de software |

> **Importante:** o trabalho é de **desenvolvimento com proposta de avaliação**.
> A validação empírica com estudantes não foi conduzida. O artigo apresenta um
> protocolo de avaliação replicável, mas não afirma eficácia comprovada — a
> distinção é deliberada e está refletida em toda a redação.

### Gamificação ou jogo educacional?

O artefato é um **jogo educacional** (serious game), não gamificação. O critério
de distinção é direto: na gamificação, elementos de jogo são acoplados a uma
atividade que continua não sendo jogo — tire os pontos de uma lista de
exercícios e a lista continua lá. No QueryQuest, **tire o SQL e não sobra jogo
nenhum**, porque a formulação da consulta *é* a ação do jogador.

Essa classificação define qual literatura fornece os parâmetros de comparação:
a meta-análise de Wouters et al. (2013), sobre serious games (d = 0,29 para
aprendizagem, d = 0,36 para retenção), e não a de Sailer e Homner (2020), que
trata de gamificação em sentido estrito.

---

## 2. O problema que o jogo ataca

A dificuldade de aprender SQL está documentada empiricamente. Taipalus, Siponen
e Vartiainen (2018) analisaram **mais de 33 mil consultas** submetidas por
estudantes de um curso introdutório e classificaram os erros em três categorias:

| Categoria | O que acontece | O SGBD avisa? |
|---|---|---|
| **Sintático** | a consulta não executa | Sim, com mensagem clara |
| **Semântico** | executa, mas o esquema não corresponde | Às vezes |
| **Lógico** | executa sem alerta e devolve resultado errado | **Não** |

A conclusão central: **os erros lógicos são os mais persistentes**, justamente
porque nada no ambiente sinaliza a falha. O estudante roda a consulta, recebe um
resultado, e segue adiante com um modelo mental equivocado.

### A lacuna

As ferramentas gamificadas de SQL existentes — SQL Island, SQLZOO, Schemaverse,
SQL Murder Mystery — compartilham uma característica: **avaliam a consulta de
forma binária**. Ou produz o resultado esperado, ou não produz. Nenhuma delas
distingue *uma consulta que apenas funciona* de *uma consulta bem formulada*.

O QueryQuest ataca exatamente esse ponto: converte a **precisão lógica** da
consulta em **consequência mecânica graduada**. `SELECT *` funciona, mas custa
caro. Uma consulta filtrada com `WHERE`, `AND` e `LIMIT` produz o mesmo feitiço
por uma fração do custo. A qualidade da formulação deixa de ser invisível.

---

## 3. Visão geral do jogo

### Estrutura

- **5 andares**, cada um com um cenário próprio e um golem elemental
- Combate **por turnos**, com posicionamento em **6 slots**
- Derrota encerra a partida e zera a build (**roguelite**)
- Vencidos os 5 andares, abre o **Modo Infinito** com inimigos escalados

### Os adversários

Dados reais do banco:

| # | Nome | Elemento | HP | Nível | Fraqueza elemental | Ataca de | Vulnerável a |
|---|---|---|---|---|---|---|---|
| 1 | Golem de Fogo | Fogo | 60 | 1 | Agua | CURTO | LONGO |
| 2 | Golem de Agua | Agua | 80 | 1 | Raio | CURTO | MEDIO |
| 3 | Golem de Terra | Terra | 120 | 2 | Vento | MEDIO | CURTO |
| 4 | Golem de Raio | Raio | 100 | 2 | Terra | MEDIO | LONGO |
| 5 | Golem Primordial | Vento | 200 | 3 | Fogo | LONGO | CURTO |

**Nenhuma dessas informações aparece na interface.** Para descobrir a fraqueza do
golem, o jogador precisa consultar a tabela `Inimigos` — o que transforma a
consulta em ação de reconhecimento, e não apenas em ataque.

### O arsenal

18 magias distribuídas em 5 elementos + neutro, em 3 níveis:

| Elemento | Nível 1 | Nível 2 | Nível 3 |
|---|---|---|---|
| Fogo | Bola de Fogo, Chama Proxima | Lanca Flamejante | Meteoro |
| Agua | Jato d'Agua | Gelo Afiado | Tsunami |
| Vento | Rajada | Vendaval Cortante | Tornado |
| Terra | Pua de Pedra | Lanca de Granito | Terremoto |
| Raio | Descarga Eletrica | Relampago Guiado | Tempestade |
| Neutro | Analise | — | Inspecionar Fragmento |

O jogador começa com as **8 magias de nível 1 mais as neutras**. As de nível 2
vêm da absorção de fragmentos via `JOIN`; as de nível 3, de páginas sorteadas
nas recompensas entre andares.

---

## 4. Arquitetura do projeto

**10.393 linhas de C#** distribuídas em 63 scripts, organizados em quatro camadas:

```
Assets/_QueryQuest/
├── Models/          esquema do banco em classes (SpellData, EnemyData, FragmentoData)
├── Database/        DatabaseManager, DatabaseSeeder, SQLInterpreter
├── Combat/          regras do jogo (CombatManager, SlotSystem, DamageCalculator…)
└── UI/              tudo o que o jogador vê (HudSkin, GrimoireUI, TabelasUI…)
```

### Convenção central: UI por código

**Toda a interface é construída em tempo de execução, por script.** As cenas
`.unity` contêm apenas o esqueleto mínimo; nenhum prefab de UI é editado à mão.

O motivo é prático: mudanças de interface viram alterações de código versionáveis
e revisáveis em diff, em vez de mudanças binárias num arquivo de cena que o Git
não sabe mesclar. O `HudSkin.cs` (1.060 linhas, o maior arquivo do projeto) é o
responsável por essa montagem.

### Os dez arquivos principais

| Arquivo | Linhas | Responsabilidade |
|---|---|---|
| `UI/HudSkin.cs` | 1060 | monta e veste toda a HUD de combate |
| `Combat/CombatManager.cs` | 585 | máquina de estados do combate |
| `UI/FragmentosUI.cs` | 558 | tela de absorção com o JOIN escrito pelo jogador |
| `UI/ArenaSceneView.cs` | 516 | cenário, personagens e efeitos visuais |
| `Database/SQLInterpreter.cs` | 367 | analisa, valida e executa as consultas |
| `UI/SmartSuggestions.cs` | 365 | autocompletar do terminal de consultas |
| `UI/MainMenuUI.cs` | 349 | menu inicial e configurações |
| `UI/ArenaUI.cs` | 288 | barra de slots e ícones de posição |
| `UI/GrimoireUI.cs` | 280 | abas e ciclo de vida do grimório |
| `UI/GrimoireSkin.cs` | 265 | superfícies do grimório geradas em runtime |

---

## 5. O banco de dados

O arquivo `queryquest.db` fica em `StreamingAssets` e é **o núcleo da mecânica**,
não um repositório acessório. Três tabelas:

### `Magias`

| Coluna | Tipo | Papel no jogo |
|---|---|---|
| `Id` | integer | identificador |
| `Nome` | varchar | é texto — no `WHERE` exige aspas simples |
| `Elemento` | varchar | Fogo, Agua, Vento, Terra, Raio, Neutro |
| `Nivel` | integer | 1, 2 ou 3 |
| `Distancia` | varchar | CURTO, MEDIO ou LONGO — alcance |
| `DanoBase` | integer | dano antes dos multiplicadores |
| `Descricao` | varchar | texto livre |
| `Desbloqueado` | integer | 1 = o jogador domina a magia |

### `Inimigos`

Mesma lógica, com as três colunas que decidem o combate: `FraquezaElemento`,
`AtaqueDistancia` e `FraquezaDistancia`.

### `Fragmentos`

20 registros. A coluna **`InimigoID` é chave estrangeira** apontando para
`Inimigos.Id` — e esse relacionamento não é ornamental: é ele que torna a
operação `JOIN` **necessária** para progredir.

### `DatabaseManager.cs` — ciclo de vida

```csharp
#if UNITY_EDITOR
    return Path.Combine(Application.streamingAssetsPath, DB_NAME);   // edição direta
#else
    // na build: copia o template para uma pasta gravável na primeira execução
    if (!File.Exists(destPath)) { ... File.Copy(sourcePath, destPath); }
    return destPath;
#endif
```

No editor o banco é usado direto de `StreamingAssets`, o que facilita inspecionar
com o DB Browser. Na build, `StreamingAssets` é somente leitura, então o arquivo
é copiado para `persistentDataPath` na primeira execução. Se não houver template,
o `DatabaseSeeder` cria e popula do zero.

---

## 6. O interpretador SQL

`SQLInterpreter.cs` é o coração pedagógico do projeto. **A consulta do jogador
não vai direto ao SQLite** — passa por análise e validação próprias.

### Por que não usar o SGBD diretamente

Porque o SGBD executa silenciosamente uma consulta logicamente equivocada. É
exatamente o ponto cego que Taipalus et al. documentaram. O interpretador, por
conhecer a intenção pedagógica, consegue dizer *o que* está errado — não apenas
que falhou.

### O pipeline

```
consulta digitada
      ↓  1. análise sintática (regex)
SELECT (colunas) FROM (tabela) [WHERE ...] [ORDER BY ...] [LIMIT n]
      ↓  2. validação semântica
tabela existe?  ·  cada coluna pertence a ela?
      ↓  3. construção parametrizada
BuildSafeQuery → SQL com placeholders + args (nunca concatenação)
      ↓  4. execução no SQLite
      ↓  5. tradução do resultado em ação de jogo
```

### Cláusulas suportadas

`SELECT` (colunas ou `*`) · `FROM` · `WHERE` com `AND`/`OR` e operadores
`=`, `!=`, `>`, `<`, `>=`, `<=`, `LIKE` · `ORDER BY` com `ASC`/`DESC` · `LIMIT`

Tabelas permitidas: `Magias` e `Inimigos`. As colunas válidas de cada uma estão
declaradas explicitamente em `ValidColumns`, o que permite recusar
`SELECT Vida FROM Magias` com uma mensagem específica em vez de um erro genérico.

### Um bug real que o projeto corrigiu

A extração do valor no `WHERE` usava `[^']*`, que quebrava em nomes com
apóstrofo. `Nome = 'Jato d'Agua'` era lido como `"Jato d"`, e como a expressão
não estava ancorada, o resto era descartado **em silêncio** — o jogador via a
consulta "funcionar" e devolver a magia errada.

A correção lê do primeiro ao último apóstrofo, aceita `''` como escape e ancora a
expressão:

```csharp
var condMatch = Regex.Match(token, @"^(\w+)\s*(=|!=|>=|<=|>|<|LIKE)\s*(.+)$",
                            RegexOptions.IgnoreCase);
```

É, ironicamente, um erro lógico do tipo que o próprio jogo se propõe a ensinar.

---

## 7. O sistema de combate

`CombatManager.cs` implementa uma **máquina de estados** com oito estados:

```
IDLE → PLAYER_TURN ⇄ GRIMOIRE_OPEN → QUERY_EXECUTING → SPELL_CAST
         ↑                                                  ↓
    CHECKING_RESULT ← APPLYING_DAMAGE ← ENEMY_TURN ←────────┘
```

Cada transição dispara `OnStateChanged`, e a UI reage a esse evento — é o que
faz o grimório fechar sozinho ao fim do combate, por exemplo.

### Cálculo de dano

`DamageCalculator.cs`:

```
Dano = DanoBase × MultiplicadorElemental × EficiênciaDeDistância
```

**Ciclo elemental** (`ElementalSystem.cs`) — cada elemento é forte contra o
anterior: Água → Fogo → Vento → Terra → Raio → Água

| Situação | Multiplicador |
|---|---|
| Super efetivo | **1,5×** |
| Neutro | 1,0× |
| Resistente (o alvo é forte contra você) | **0,75×** |

**Eficiência de distância** — cada magia rende mais no alcance para o qual foi
feita:

| Magia \ Distância atual | CURTO | MEDIO | LONGO |
|---|---|---|---|
| **CURTO** | 1,0 | 0,6 | 0,3 |
| **MEDIO** | 0,6 | 1,0 | 0,6 |
| **LONGO** | 0,3 | 0,6 | 1,0 |

O resultado combinado significa que a mesma magia pode causar de **0,3×** a
**1,5×** o dano base conforme elemento e posicionamento — uma variação de **5
vezes** decidida inteiramente por informação que está no banco.

### A magia `Analise` — mana por coluna

Mecânica distintiva: `Analise` revela atributos do inimigo, e o **custo é
proporcional ao número de colunas consultadas**.

```csharp
int columnsAnalyzed = (colsPart == "*") ? ALL_COLUMNS : colsPart.Split(',').Length;
int manaCost = columnsAnalyzed * COST_PER_COLUMN;
```

`SELECT * FROM Inimigos` revela tudo, mas cobra por todas as colunas.
`SELECT FraquezaElemento FROM Inimigos` cobra por uma. **A projeção deixa de ser
detalhe de estilo e vira decisão econômica** — que é precisamente a intuição que
se quer construir sobre `SELECT`.

---

## 8. Economia de mana: o retorno graduado

`ManaSystem.cs` — o mecanismo central da proposta pedagógica.

Toda consulta parte de um **custo base de 45**, do qual são descontados valores
conforme o refinamento empregado:

| Cláusula | Desconto |
|---|---|
| `WHERE` | −10 |
| Cada `AND` / `OR` adicional | −8 |
| `ORDER BY` | −12 |
| `LIMIT` | −6 |
| Cada operador de comparação | −5 |
| **Piso** | 8 (nunca é grátis) |

### O que isso ensina

```sql
-- 45 de mana: funciona, mas você pagou pela tabela inteira
SELECT * FROM Magias

-- 45 − 10 − 5 − 12 − 6 = 12 de mana: mesma magia, um quarto do custo
SELECT Nome FROM Magias WHERE Elemento = 'Fogo' ORDER BY DanoBase DESC LIMIT 1
```

As duas consultas lançam o mesmo feitiço. A diferença é que a segunda **custa
quase quatro vezes menos** — e essa diferença é sentida imediatamente, porque
mana é o recurso que decide se você sobrevive ao andar.

É esta a conversão que o trabalho propõe: **a precisão lógica, que o SGBD não
avalia, vira consequência mecânica imediata e perceptível.**

---

## 9. Posicionamento e distância

`SlotSystem.cs` — a arena tem **6 slots**. Jogador começa no slot 1, inimigo no 6.

```
[1] [2] [3] [4] [5] [6]
 P                   E
```

- A cada turno o jogador pode **recuar, manter ou avançar** uma casa
- A distância é `|EnemySlot − PlayerSlot|`, e determina a faixa CURTO/MEDIO/LONGO
- O inimigo se move em direção ao jogador conforme sua própria IA
- `SpellHitsEnemy(distancia)` decide se a magia sequer alcança o alvo

O posicionamento é a **segunda camada tática**: não basta acertar o elemento, é
preciso estar na distância certa. E como cada golem tem uma `FraquezaDistancia`
distinta, a posição ideal muda a cada andar — informação que, de novo, só existe
no banco.

---

## 10. Progressão: fragmentos, JOIN e roguelite

### A cena de absorção

Ao derrotar um golem, ele deixa cair **fragmentos da própria essência**
(`FragmentDropSystem.cs`, com chances por raridade: 80%, 60%, 40%, 20%, 5%, e
pelo menos um drop garantido).

Para absorver, o jogador precisa **escrever um JOIN**:

```sql
SELECT f.Nome, i.Nome AS Inimigo
FROM Fragmentos f
JOIN Inimigos i ON f.InimigoID = i.Id
WHERE f.FragmentoID = 3
```

`FragmentosUI.cs` valida a consulta com mensagens específicas para cada erro:

| Erro | Mensagem devolvida |
|---|---|
| falta o JOIN | "Falta trazer a outra tabela: JOIN Inimigos" |
| `ON` ligando colunas erradas | "A ligacao certa e InimigoID (de Fragmentos) = Id (de Inimigos)" |
| sem filtrar o fragmento | "Filtre o fragmento escolhido: WHERE f.FragmentoID = N" |
| `;` ou DDL/DML | recusado — só leitura |

Se a consulta passa, **ela é executada de verdade no SQLite** e o resultado real
aparece antes da recompensa: a magia de nível 2 daquele elemento. Um botão
**DICA** oferece ajuda em três degraus — o conceito, a coluna que liga as
tabelas, e por fim o exemplo completo.

**O domínio de `JOIN` é condição de progressão, não conteúdo opcional.**

### A estrutura roguelite

`FloorManager.cs`:

- derrota → `RestartRun()` zera atributos, limpa fragmentos e re-tranca as magias
  elementais de nível ≥ 2
- entre andares, `RewardGenerator` sorteia **3 recompensas** e o jogador escolhe 1
  (Armadura, Cajado elemental ou Página de magia)
- vencidos os 5 andares, `StartInfiniteMode()` recomeça com HP escalado
  (`× (1 + 0,6 × volta)`) preservando a build

### Por que roguelite importa pedagogicamente

Perder e recomeçar significa **formular as mesmas consultas repetidamente, com
variação de contexto e custo emocional baixo** — perder faz parte do formato, não
é punição. Prática distribuída e prática de recuperação estão entre as técnicas
de estudo de maior utilidade comprovada (Dunlosky et al., 2013), e aqui são
exercidas de forma incidental, sem o estudante precisar organizar uma rotina de
revisão.

---

## 11. A interface

### `HudSkin.cs` — o montador

Roda em `RuntimeInitializeOnLoadMethod(AfterSceneLoad)` e reconstrói a HUD sobre
o esqueleto da cena. Um detalhe que custou caro descobrir:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
private static void AutoCreate()
{
    // ATENÇÃO: este atributo roda UMA VEZ por execução, não a cada cena.
    Criar();
    SceneManager.sceneLoaded -= AoCarregarCena;
    SceneManager.sceneLoaded += AoCarregarCena;   // por isso, re-registra
}
```

Sem o re-registro, ao adicionar o menu como cena 0 a HUD do combate **nunca seria
vestida**, porque o atributo já teria disparado na cena anterior.

### Posicionamento por fração da arte

As barras de vida, os slots e os botões não usam coordenadas em pixels: usam
**frações medidas na própria imagem**. Exemplo real, medido nos pixels da arte:

```csharp
private static readonly Vector4 SlotVoltar = new Vector4(0.1886f, 0.3377f, 0.6755f, 0.8929f);
```

Isso garante que os controles caiam dentro das caixas desenhadas em qualquer
resolução, de 1024×768 a 4K, incluindo ultrawide.

### A barra inferior que nunca estica

A arte traz o arco central e as caixas dos botões já desenhados, então esticar
deformaria tudo. A solução:

```csharp
public static Vector2 TamanhoDaBarra(Vector2 tela)
{
    float escala = Mathf.Min(tela.x * 0.62f / ArteBaixoW,
                             tela.y * 0.34f / ArteBaixoH);
    return new Vector2(ArteBaixoW * escala, ArteBaixoH * escala);
}
```

A proporção da arte manda; o menor entre 62% da largura e 34% da altura decide o
tamanho. Validado em 10 resoluções, com desvio de proporção de **0,000%**.

### O grimório

`GrimoireUI.cs` + `GrimoireSkin.cs` + três abas:

- **QUERY** — terminal onde a consulta é digitada, com autocompletar
- **MAGIAS** (`MagiasUI.cs`) — cartões por elemento, com nível, alcance, dano e
  estado de desbloqueio
- **TABELAS** (`TabelasUI.cs`) — a documentação do banco

As superfícies do grimório são **geradas em runtime** — retângulos de cantos
arredondados com borda dourada, em 9-slice — porque as artes da HUD são banners
largos com ornamento nas pontas, que deformariam num painel alto e estreito.

O grimório tem `Canvas` próprio com `overrideSorting` (ordem 500) e
`GraphicRaycaster`, o que o coloca acima de toda a HUD, e é arrastável pelo
cabeçalho.

### A documentação que não pode mentir

`SchemaGuia.cs` lê as colunas **do esquema real do banco**, via
`pragma table_info`:

```csharp
info = db.Query<LinhaPragma>($"pragma table_info(\"{tabela}\")");
```

Se o banco mudar, a documentação da aba TABELAS muda junto. Não há como divergir
do jogo — o que importa num material que o aluno consulta enquanto joga.

---

## 12. Sistemas de apoio

| Sistema | Arquivo | O que faz |
|---|---|---|
| **Espírito da lua** | `SpiritCompanion.cs` | monta 3 dicas por combate sobre a fraqueza do golem; clicar na lua cicla entre elas. As dicas são **leves** — apontam a direção, nunca entregam o dado cru |
| **Tutorial** | `TutorialPopup.cs` | 3 páginas com o essencial, reabríveis pelo botão AJUDA |
| **Log de combate** | `CombatLogUI.cs` | registro rolável com o detalhamento de cada cálculo de dano |
| **Autocompletar** | `SmartSuggestions.cs` | sugere tabelas, colunas e cláusulas enquanto o jogador digita |
| **Animação** | `SpriteAnimator.cs` + libraries | 5 poses do protagonista, 4 de cada golem, com `flipX` por animação |
| **VFX** | `SpellVFXPlayer.cs` | projétil e explosão recoloridos por elemento e escalados por nível (0,75× / 1,2× / 1,8×) |
| **Sessão** | `GameSession.cs` | guarda o andar para o "Continuar" do menu |

---

## 13. Fluxo completo de uma partida

```
1.  MainMenu → NOVO JOGO
2.  Tutorial (3 páginas)
3.  ANDAR 1 — Golem de Fogo, fraquezas desconhecidas
        ↓
4.  A lua dá uma dica leve
        ↓
5.  Abre o grimório (aba TABELAS para consultar o esquema)
        ↓
6.  SELECT FraquezaElemento FROM Inimigos     ← descobre: Agua
        · custo proporcional às colunas pedidas
        ↓
7.  SELECT * FROM Magias WHERE Elemento = 'Agua' AND Distancia = 'LONGO'
        · a consulta filtrada custa menos
        · a magia devolvida é lançada
        ↓
8.  Dano = DanoBase × 1,5 (elemental) × eficiência de distância
        ↓
9.  Movimenta-se (recuar/manter/avançar) e clica ENCERRAR TURNO
        · 1 magia + 1 movimento por turno
        ↓
10. Turno do inimigo → repete até alguém cair
        ↓
11. Vitória → cena de absorção: escreve o JOIN
        · acerta → magia de nível 2 daquele elemento
        ↓
12. Tela de recompensa: escolhe 1 entre 3
        ↓
13. ANDAR 2 … até o 5
        ↓
14. Vitória final → MODO INFINITO (inimigos escalados, build mantida)

    (derrota em qualquer ponto → a run acaba e tudo recomeça do andar 1)
```

---

## 14. Decisões técnicas e seus porquês

### Interpretador próprio em vez do SGBD direto

Um SGBD executa silenciosamente consultas logicamente equivocadas. O
interpretador conhece a intenção pedagógica e devolve retorno específico. É a
razão de existir do trabalho, traduzida em código.

### SQLite embarcado

Sem servidor, sem internet, sem configuração. O jogo roda em qualquer laboratório
de informática a partir de uma pasta copiada. `sqlite-net` faz o mapeamento
objeto-relacional por atributos `[Table]` e `[Column]`.

### Consultas parametrizadas

`BuildSafeQuery` monta o SQL com placeholders e argumentos separados, nunca por
concatenação — mesmo sendo um banco local. É um jogo sobre SQL: seria contraditório
ensinar pelo exemplo uma prática vulnerável a injeção.

### O turno não passa sozinho

Lançar magia não encerra o turno; só o botão **ENCERRAR TURNO** encerra. Isso
**dissocia o tempo de reflexão sobre a consulta do tempo de reação do combate** —
o estudante pode formular, revisar e corrigir sem penalização temporal. Regra:
uma magia e um movimento por turno.

### UI por código

Mudanças de interface viram diffs revisáveis em vez de alterações binárias em
arquivos de cena que o Git não mescla.

---

## 15. Validação

O projeto foi validado por **execução automatizada em play mode**, via scripts de
editor descartáveis rodando em batch mode. Não são testes unitários: são
verificações do jogo rodando de verdade, medindo valores reais em tempo de
execução.

Exemplos do que foi verificado:

- **Interpretador**: 7 consultas de teste, incluindo nomes com apóstrofo
- **Absorção**: consulta sem JOIN é recusada **sem consumir o fragmento**;
  consulta correta libera a magia e cobra 20 de mana
- **Barra inferior**: proporção idêntica à da arte (desvio 0,000%) em 10
  resoluções, de 1024×768 a 4K e 21:9
- **Grimório**: hover do livro alterna os sprites; abre em estado `IDLE`;
  arrasta pelo cabeçalho
- **Layout**: nenhum par de elementos se sobrepõe na tela de absorção

### Uma lição de método

Uma versão anterior do teste de rolagem escrevia `verticalNormalizedPosition`
direto no componente. O teste passava, mas **a rolagem não funcionava para o
jogador** — porque escrever a propriedade contorna todo o caminho de input, e o
defeito estava justamente ali: sem um `Graphic` com `raycastTarget` sob o cursor,
o `ScrollRect` nunca recebia o evento da roda.

O teste media o efeito, não a causa. A versão corrigida dispara `OnScroll` com um
`PointerEventData` real e confere, com `RaycastAll`, que o ponteiro alcança o
componente.

---

## 16. Limitações conhecidas

| Limitação | Situação |
|---|---|
| **Avaliação empírica** | não conduzida. O artigo propõe protocolo replicável, sem afirmar eficácia |
| **`GROUP BY` e agregação** | não implementados. O jogo cobre `SELECT`, `FROM`, `WHERE`, `AND`/`OR`, operadores, `ORDER BY`, `LIMIT` e `JOIN` |
| **DDL e DML** | fora do escopo — o jogo é somente leitura |
| **Fases fixas** | os 5 andares são pré-definidos; não há geração procedural |
| **Plataforma** | Windows x64 apenas |

### Trabalhos futuros

1. Aplicar o protocolo de avaliação delineado no artigo
2. Incorporar mecânica de agregação (`GROUP BY` + `HAVING`) — o desenho já
   existe: fundir fragmentos do mesmo elemento exigiria
   `GROUP BY Elemento HAVING COUNT(*) >= 3` para liberar a magia de nível 3
3. Investigar retenção em prazo mais longo, com teste posterior ao encerramento
   da disciplina

---

## Referências do embasamento

- **CODD, E. F.** (1970) — modelo relacional
- **CONNOLLY, T. M. et al.** (2012) — revisão sistemática de serious games
- **DETERDING, S. et al.** (2011) — definição de gamificação
- **DICHEVA, D. et al.** (2015) — mapeamento sistemático de gamificação na educação
- **DUNLOSKY, J. et al.** (2013) — técnicas de estudo eficazes
- **SAILER, M.; HOMNER, L.** (2020) — meta-análise de gamificação
- **SCHILDGEN, J.** (2014) — SQL Island
- **TAIPALUS, T. et al.** (2018) — erros na formulação de consultas SQL
- **WOUTERS, P. et al.** (2013) — meta-análise de serious games

Referências completas em ABNT no artigo.

---

## Documentos relacionados

| Arquivo | Conteúdo |
|---|---|
| `BUILD.md` | como gerar o executável e levá-lo para outra máquina |
| `QueryQuest - Artigo (modelo UGB).docx` | o artigo científico no modelo da instituição |
