# QueryQuest — Relatório Técnico Completo

Documentação integral do projeto: da proposta acadêmica à implementação, com o
funcionamento detalhado de cada sistema. Escrita para servir de material de
estudo para a defesa do TCC — cada seção termina no "porquê", não só no "o quê".

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
15. [Dois bugs reais e o que ensinam](#15-dois-bugs-reais-e-o-que-ensinam)
16. [Validação](#16-validação)
17. [Limitações conhecidas](#17-limitações-conhecidas)
18. [Perguntas prováveis da banca](#18-perguntas-prováveis-da-banca)

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
> protocolo de avaliação replicável (pré-teste, pós-teste e questionário Likert),
> mas não afirma eficácia comprovada — a distinção é deliberada e está refletida
> em toda a redação. **Não confundir com MEEGA+**: o protocolo do artigo é
> pré/pós-teste próprio, não o instrumento MEEGA+.

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

As ferramentas gamificadas de SQL existentes — SQL Island (Schildgen, 2014),
SQLZOO, Schemaverse, SQL Murder Mystery — compartilham uma característica:
**avaliam a consulta de forma binária**. Ou produz o resultado esperado, ou não
produz. Nenhuma delas distingue *uma consulta que apenas funciona* de *uma
consulta bem formulada*.

O QueryQuest ataca exatamente esse ponto: converte a **precisão lógica** da
consulta em **consequência mecânica graduada**. `SELECT *` funciona, mas custa
caro. Uma consulta filtrada com `WHERE`, `AND` e `LIMIT` produz o mesmo feitiço
por uma fração do custo. A qualidade da formulação deixa de ser invisível — e,
desde a última iteração da interface, **deixa de ser abstrata**: uma barra
visual mostra o custo caindo em tempo real, antes mesmo de executar (seção 8).

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

**≈ 11.700 linhas de C# próprio**, em **66 scripts**, sem contar o plugin de
terceiros `SQLite.cs` (a biblioteca sqlite-net, 5.445 linhas). Organizados em
quatro camadas:

```
Assets/_QueryQuest/
├── Models/          esquema do banco em classes (SpellData, EnemyData, FragmentoData)
├── Database/         DatabaseManager, DatabaseSeeder, SQLInterpreter
├── Combat/          regras do jogo (CombatManager, SlotSystem, DamageCalculator…)
└── UI/              tudo o que o jogador vê (HudSkin, GrimoireUI, TabelasUI…)
```

### Convenção central: UI por código

**Toda a interface é construída em tempo de execução, por script.** As cenas
`.unity` contêm apenas o esqueleto mínimo; nenhum prefab de UI é editado à mão.

O motivo é prático: mudanças de interface viram alterações de código versionáveis
e revisáveis em diff, em vez de mudanças binárias num arquivo de cena que o Git
não sabe mesclar. O `HudSkin.cs` (1.088 linhas, o maior arquivo do projeto) é o
responsável por essa montagem.

### Os arquivos principais

| Arquivo | Linhas | Responsabilidade |
|---|---|---|
| `UI/HudSkin.cs` | 1088 | monta e veste toda a HUD de combate |
| `UI/FragmentosUI.cs` | 759 | tela de absorção com o JOIN escrito pelo jogador |
| `UI/ArenaSceneView.cs` | 617 | cenário, personagens, caminhada e efeitos visuais |
| `Combat/CombatManager.cs` | 579 | máquina de estados do combate |
| `UI/SmartSuggestions.cs` | 397 | autocompletar do terminal de consultas |
| `UI/QueryPanelSkin.cs` | 381 | layout da aba QUERY e a barra de custo de mana |
| `Database/Scripts/SQLInterpreter.cs` | 367 | analisa, valida e executa as consultas |
| `UI/MainMenuUI.cs` | 349 | menu inicial e configurações |
| `UI/ArenaUI.cs` | 288 | barra de slots e ícones de posição |
| `UI/GrimoireUI.cs` | 280 | abas e ciclo de vida do grimório |
| `UI/GrimoireSkin.cs` | 265 | superfícies do grimório geradas em runtime |
| `UI/CombatLogUI.cs` | 265 | registro de combate |
| `UI/MagiasUI.cs` | 263 | aba de magias do grimório |
| `UI/RewardScreenUI.cs` | 256 | tela de recompensa pós-andar |

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
operação `JOIN` **necessária** para progredir. Colunas relevantes:
`FragmentoID` (chave), `InimigoID`, `Nome`, `Elemento`, `Tipo`, `Raridade`
(1 a 5), `Descricao`, `ValorXP`, `Pista`.

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

**Detalhe de infraestrutura que já causou um incidente real**: o jogo usa o
plugin nativo `sqlite3.dll` (x86_64), carregado por `sqlite-net` via P/Invoke.
Uma versão anterior da DLL era **ARM64**, incompatível com qualquer PC comum —
o jogo só funcionava por acidente no editor, porque o Mono caía para uma
`sqlite3.dll` x64 achada no PATH do sistema (a do miniconda). Corrigido
substituindo a DLL por uma build x86_64 real, verificada por cabeçalho PE.

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
· `JOIN ... ON` (apenas na tela de absorção de fragmentos, seção 10)

Tabelas permitidas no combate: `Magias` e `Inimigos`. As colunas válidas de
cada uma estão declaradas explicitamente em `ValidColumns`, o que permite
recusar `SELECT Vida FROM Magias` com uma mensagem específica em vez de um
erro genérico.

**Ainda não suportado**: `GROUP BY`, `HAVING`, funções de agregação
(`COUNT`, `SUM`, `AVG`...), `INSERT`/`UPDATE`/`DELETE`/DDL. O jogo é
estritamente de leitura (`SELECT`). Ver seção 17.

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

Mecânica distintiva: `Analise` (`FROM Inimigos`) revela atributos do inimigo, e
o **custo é proporcional ao número de colunas consultadas**. A conta mora em
`ManaSystem.cs` — não em `CombatManager` — justamente para que a barra de custo
visual (seção 8) e a cobrança real nunca possam divergir:

```csharp
// ManaSystem.cs
public const int CUSTO_POR_COLUNA   = 8;
public const int COLUNAS_DO_INIMIGO = 7; // Nome, Nivel, Elemento, HP, Fraqueza, AtaqueDistancia, FraquezaDistancia

public static int ColunasPedidas(string rawQuery)
{
    var m = Regex.Match(rawQuery, @"SELECT\s+(.+?)\s+FROM", RegexOptions.IgnoreCase);
    string cols = m.Success ? m.Groups[1].Value.Trim() : "*";
    return cols == "*" ? COLUNAS_DO_INIMIGO : cols.Split(',').Length;
}
```

`SELECT * FROM Inimigos` revela tudo, mas cobra pelas 7 colunas (56 de mana).
`SELECT FraquezaElemento FROM Inimigos` cobra por uma (8 de mana). **A projeção
deixa de ser detalhe de estilo e vira decisão econômica** — que é precisamente
a intuição que se quer construir sobre `SELECT`.

---

## 8. Economia de mana: o retorno graduado

`ManaSystem.cs` — o mecanismo central da proposta pedagógica.

Toda consulta de **magia** (`FROM Magias`) parte de um **custo base de 45**, do
qual são descontados valores conforme o refinamento empregado:

| Cláusula | Desconto |
|---|---|
| `WHERE` | −10 |
| Cada `AND` / `OR` adicional | −8 |
| `ORDER BY` | −12 |
| `LIMIT` | −6 |
| Cada operador de comparação | −5 |
| **Piso** | 8 (nunca é grátis) |

Consultas de **análise** (`FROM Inimigos`) seguem a regra à parte descrita na
seção 7 — mana por coluna revelada, não por especificidade da cláusula.

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

### A barra de custo (mecânica visual)

Na aba QUERY do grimório, uma **barra de mana** (`QueryPanelSkin.cs` +
`ManaCostMeter`) mostra o preço da magia **antes** de executar. Ela nasce
cheia no custo máximo e esvazia em tempo real, a cada tecla digitada:

```csharp
// ManaCostMeter.Atualizar — chamado a cada onValueChanged do campo de consulta
var previsao = ManaSystem.Prever(consulta);   // mesma conta que vai cobrar de verdade
_fill.fillAmount = previsao.Fracao;           // 1,0 = custo máximo, perto de 0 = consulta afiada
_fill.color = falta ? Vermelho : Azul;        // fica vermelha se a mana não dá
```

`ManaSystem.Prever()` decide sozinho se a consulta é magia ou análise e aplica
a conta correspondente — é a **mesma função** usada na cobrança real
(`CombatManager.SubmitQuery` / `HandleAnalise`), então a barra nunca promete um
preço e cobra outro. É a conversão da seção 2 ("precisão lógica vira
consequência mecânica graduada") deixando de ser abstrata: o aluno vê o
número caindo *enquanto* escreve o `WHERE`, não só depois de errar.

Progressão observada em teste:

| Consulta | Custo | Barra |
|---|---|---|
| `SELECT * FROM Magias` | 45 | 100% |
| `... WHERE Elemento = 'Fogo'` | 35 | 78% |
| `... AND Nivel = 1` | 27 | 60% |
| `... ORDER BY DanoBase LIMIT 1` | 9 | 20% |

É esta a conversão que o trabalho propõe: **a precisão lógica, que o SGBD não
avalia, vira consequência mecânica imediata e perceptível — e agora também
visível antes mesmo de agir.**

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

**Detalhe visual, sem efeito na regra**: mover não é mais um corte seco. O
token percorre a distância entre slots (`ArenaSceneView.Caminhar`), e no caso
do herói o percurso dura exatamente o tempo da animação "andando" (25 quadros
a 18 fps ≈ 1,39s), lido por `SpriteAnimator.DuracaoDe("andando")` — os passos
da animação batem com o avanço real, em vez do personagem "patinar" no lugar
enquanto teleporta para o destino.

---

## 10. Progressão: fragmentos, JOIN e roguelite

### A cena de absorção

Ao derrotar um golem, ele deixa cair **fragmentos da própria essência**
(`FragmentDropSystem.cs`), com chance por raridade:

| Raridade | 1 (comum) | 2 | 3 | 4 | 5 (lendário) |
|---|---|---|---|---|---|
| Chance | 80% | 60% | 40% | 20% | 5% |

Pelo menos um fragmento é sempre garantido, mesmo que a rolagem falhe em todos.

**Se mais de um fragmento cai, o jogador escolhe qual absorver primeiro** — a
tela de absorção (`FragmentosUI.cs`) lista todos os fragmentos do inventário
(inclusive sobras de andares anteriores, com os "novos" deste combate
marcados), e o jogador clica em um para selecioná-lo antes de escrever o JOIN.
Isso importa porque cada fragmento pode conceder um elemento diferente — a
escolha entre dois fragmentos é uma decisão estratégica, não um detalhe de
inventário, e o painel mostra a magia que seria concedida **antes** de gastar
mana em absorver.

Para absorver o fragmento selecionado, o jogador precisa **escrever um JOIN**:

```sql
SELECT f.Nome, i.Nome AS Inimigo
FROM Fragmentos f
JOIN Inimigos i ON f.InimigoID = i.Id
WHERE f.FragmentoID = 3
```

`FragmentosUI.ValidarJoin` valida a consulta com mensagens específicas para cada erro:

| Erro | Mensagem devolvida |
|---|---|
| falta o `SELECT`/`FROM Fragmentos` | "Comece pela tabela do item: FROM Fragmentos" |
| falta o JOIN | "Falta trazer a outra tabela: JOIN Inimigos" |
| `ON` ligando colunas erradas | "A ligacao certa e InimigoID (de Fragmentos) = Id (de Inimigos)" |
| sem filtrar o fragmento | "Filtre o fragmento escolhido: WHERE f.FragmentoID = N" |
| `;` ou DDL/DML | recusado — só leitura |

Se a consulta passa, **ela é executada de verdade no SQLite** e o resultado
real aparece antes da recompensa: a magia de nível 2 daquele elemento.
Absorver custa **20 de mana** (`CustoMana`).

Um botão **DICA** oferece ajuda em três degraus, e **cada degrau custa 10 de
mana** (`CustoDica`) — reexibir uma dica já paga é grátis; só avançar de degrau
cobra. Isso transforma a ajuda num recurso administrado, e não numa muleta
gratuita:

1. *(-10 mana)* aponta que a informação está em **duas tabelas**
2. *(-10 mana)* revela a coluna-ponte (`InimigoID` → `Id`) e o `WHERE` exato
3. *(grátis)* exemplo completo da consulta

**O domínio de `JOIN` é condição de progressão, não conteúdo opcional.**

### A estrutura roguelite

`FloorManager.cs`:

- derrota → `RestartRun()` zera atributos, limpa fragmentos e re-tranca as magias
  elementais de nível ≥ 2
- entre andares, `RewardGenerator` sorteia **3 recompensas** e o jogador escolhe 1:

  | Tipo | Efeito mecânico |
  |---|---|
  | Armadura (`Armor`) | −12% de dano recebido |
  | Cajado elemental (`Staff`) | +25% de dano com o elemento associado |
  | Página de magia (`Page`) | desbloqueia uma magia específica |

  A tela de recompensa separa visualmente o **efeito mecânico** (o número que
  importa) da descrição narrativa, para que a escolha seja informada.

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

As barras de vida, os slots, os botões e (mais recentemente) toda a aba QUERY
e a tela de absorção de fragmentos não usam coordenadas em pixels: usam
**frações medidas na própria imagem/painel**. Exemplo real, medido nos pixels
da arte:

```csharp
private static readonly Vector4 SlotVoltar = new Vector4(0.1886f, 0.3377f, 0.6755f, 0.8929f);
```

Isso garante que os controles caiam dentro das caixas desenhadas em qualquer
resolução, de 1024×768 a 4K, incluindo ultrawide. Onde o conteúdo é variável
(nome/descrição de fragmentos vindos do banco, texto de dicas que muda de
tamanho a cada degrau), a posição por fração é combinada com **autoajuste de
fonte** (`TextMeshProUGUI.enableAutoSizing`), que encolhe o texto até caber em
vez de depender do cálculo estar certo.

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

- **QUERY** (`QueryTerminalUI.cs` + `QueryPanelSkin.cs`) — terminal onde a
  consulta é digitada, com autocompletar (`SmartSuggestions.cs`) e a **barra de
  custo de mana** (seção 8) no topo
- **MAGIAS** (`MagiasUI.cs`) — cartões por elemento, com nível, alcance, dano e
  estado de desbloqueio
- **TABELAS** (`TabelasUI.cs`) — a documentação do banco

As superfícies do grimório (fundo, cabeçalho, abas, cartões) são **geradas em
runtime** por `GrimoireSkin.cs` — retângulos de cantos arredondados com borda
dourada, em 9-slice — porque as artes da HUD são banners largos com ornamento
nas pontas, que deformariam num painel alto e estreito.

O grimório tem `Canvas` próprio com `overrideSorting` (ordem 500) e
`GraphicRaycaster`, o que o coloca acima de toda a HUD (inclusive sobre a tela
de absorção de fragmentos, que roda fora do turno de jogador), e é arrastável
pelo cabeçalho.

Todo texto do grimório e do registro de combate recebe **contorno** aplicado
via material compartilhado (`TextOutline.cs`) — a cor do contorno é escolhida
pelo tom do texto (contorno claro sobre tinta escura, escuro sobre texto
claro), para o texto se destacar do pergaminho sem custar um material por
linha de log.

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
| **Autocompletar** | `SmartSuggestions.cs` | sugere tabelas, colunas e cláusulas enquanto o jogador digita, em chips clicáveis que se inserem no campo |
| **Barra de custo** | `QueryPanelSkin.cs` (`ManaCostMeter`) | mostra o preço da magia/análise em tempo real, antes de executar (seção 8) |
| **Animação** | `SpriteAnimator.cs` + libraries | 5 poses do protagonista (idle, andando, ataque, dano, grimório), 4 de cada golem, com `flipX` por animação e duração exposta via `DuracaoDe()` |
| **Caminhada** | `ArenaSceneView.cs` | desliza o token entre slots em vez de teletransportar; a duração casa com a animação de andar |
| **VFX** | `SpellVFXPlayer.cs` | projétil e explosão recoloridos por elemento e escalados por nível (0,75× / 1,2× / 1,8×) |
| **Contorno de texto** | `TextOutline.cs` | contorno por material compartilhado, para legibilidade sobre pergaminho/madeira sem custo por linha |
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
        · custo proporcional às colunas pedidas (mostrado na barra)
        ↓
7.  SELECT * FROM Magias WHERE Elemento = 'Agua' AND Distancia = 'LONGO'
        · a barra de custo esvazia enquanto o WHERE é digitado
        · a consulta filtrada custa menos
        · a magia devolvida é lançada
        ↓
8.  Dano = DanoBase × 1,5 (elemental) × eficiência de distância
        ↓
9.  Movimenta-se (recuar/manter/avançar) — o herói CAMINHA até o slot,
    sem teleporte — e clica ENCERRAR TURNO
        · 1 magia + 1 movimento por turno
        ↓
10. Turno do inimigo → repete até alguém cair
        ↓
11. Vitória → cena de absorção: escolhe o fragmento (se caiu mais de um)
    e escreve o JOIN
        · acerta → magia de nível 2 daquele elemento
        · cada dica custa 10 de mana; absorver custa 20
        ↓
12. Tela de recompensa: escolhe 1 entre 3 (Armadura / Cajado / Página)
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

### A mesma função calcula e cobra

O custo mostrado na barra de mana e o custo cobrado na execução vêm da **mesma
chamada** (`ManaSystem.Prever` / `CalculateCost`). Foi deliberado: duas
implementações da mesma regra sempre divergem na primeira mudança — e aqui a
regra *é* o conteúdo pedagógico, então divergir seria ensinar errado.

---

## 15. Dois bugs reais e o que ensinam

Vale conhecer os dois de cor: são bons exemplos concretos de rigor técnico para
citar na defesa, e o primeiro é ironicamente do **mesmo tipo de erro** que o
jogo se propõe a ensinar.

### O apóstrofo silencioso (`SQLInterpreter.cs`)

A extração do valor no `WHERE` usava `[^']*`, que quebrava em nomes com
apóstrofo. `Nome = 'Jato d'Agua'` era lido como `"Jato d"`, e como a expressão
não estava ancorada, o resto era descartado **em silêncio** — o jogador via a
consulta "funcionar" e devolver a magia errada.

A correção lê do primeiro ao último apóstrofo e ancora a expressão:

```csharp
var condMatch = Regex.Match(token, @"^(\w+)\s*(=|!=|>=|<=|>|<|LIKE)\s*(.+)$",
                            RegexOptions.IgnoreCase);
```

É, ironicamente, um **erro lógico** do tipo que o próprio jogo se propõe a
ensinar: executou sem alerta e devolveu resultado errado.

### A animação que corria mais rápido que o cronômetro (`SpriteAnimator.cs`)

Ao sincronizar a caminhada do herói com a distância percorrida na arena (seção
9), a animação usava `WaitForSeconds(1f / fps)` por quadro. O problema: o
Unity só retoma a corrotina no **próximo** quadro *depois* do prazo vencer —
a 60 fps, cada quadro de animação de 1/18s (≈ 0,0556s) na prática custava
1/15s (≈ 0,0667s), porque esperava o próximo tick de 60 fps disponível depois
do prazo. Ao longo de 25 quadros, o erro acumulado fazia a animação de
"andando" durar cerca de **20% a mais** do que o valor nominal
(`quadros / fps`) — exatamente o valor usado para calibrar a duração do
deslize do personagem pela arena.

Resultado visível: o personagem chegava ao slot de destino e continuava
"andando no lugar" por uma fração de segundo, porque o deslocamento (calibrado
pela duração nominal) terminava antes da animação (que rodava mais devagar na
prática).

Corrigido trocando `WaitForSeconds` por acumulação de tempo por quadro
(`Time.deltaTime`), pulando frames quando necessário em vez de atrasar:

```csharp
acumulado += Time.deltaTime;
while (acumulado >= frameTime)       // while, não if: pula frame em vez de atrasar
{
    acumulado -= frameTime;
    i++;
    _image.sprite = anim.frames[i];
}
```

O paralelo com o bug do apóstrofo é direto: em ambos os casos, **o sistema
"funcionava" e produzia um resultado sutilmente errado, sem erro nenhum na
tela** — só perceptível medindo o valor real, não o esperado.

---

## 16. Validação

O projeto foi validado por **execução automatizada em play mode**, via scripts de
editor descartáveis rodando em batch mode. Não são testes unitários: são
verificações do jogo rodando de verdade, medindo valores reais em tempo de
execução — nunca lendo/escrevendo uma propriedade interna que contorne o
caminho real do jogador (ver "lição de método" abaixo).

Exemplos do que foi verificado:

- **Interpretador**: consultas de teste, incluindo nomes com apóstrofo
- **Absorção**: consulta sem JOIN é recusada **sem consumir o fragmento**;
  consulta correta libera a magia e cobra 20 de mana
- **Barra de custo de mana**: progressão medida em play mode — 45 → 35 → 27 → 9
  de mana conforme a consulta ganha `WHERE`, `AND`, `ORDER BY` e `LIMIT`,
  com a fração da barra caindo de 1,000 a 0,200 na mesma proporção
- **Barra inferior**: proporção idêntica à da arte (desvio 0,000%) em 10
  resoluções, de 1024×768 a 4K e 21:9
- **Grimório**: hover do livro alterna os sprites; abre em estado `IDLE`;
  arrasta pelo cabeçalho; 71 textos internos confirmados com contorno aplicado
- **Layout**: nenhum par de elementos se sobrepõe na tela de absorção nem na
  aba QUERY; nenhum texto extrapola sua caixa (medido com `textBounds` contra
  o `rect` local do componente, nunca em coordenadas de tela)

### Uma lição de método

Uma versão anterior do teste de rolagem escrevia `verticalNormalizedPosition`
direto no componente. O teste passava, mas **a rolagem não funcionava para o
jogador** — porque escrever a propriedade contorna todo o caminho de input, e o
defeito estava justamente ali: sem um `Graphic` com `raycastTarget` sob o cursor,
o `ScrollRect` nunca recebia o evento da roda.

O teste media o efeito, não a causa. A versão corrigida dispara `OnScroll` com um
`PointerEventData` real e confere, com `RaycastAll`, que o ponteiro alcança o
componente. A mesma disciplina se repetiu depois: medir a barra de mana lendo
`ManaSystem.Prever()` e o `fillAmount` real do componente, não assumir que a
fórmula "deveria" bater.

---

## 17. Limitações conhecidas

| Limitação | Situação |
|---|---|
| **Avaliação empírica** | não conduzida. O artigo propõe protocolo replicável (pré/pós-teste + Likert), sem afirmar eficácia |
| **Registro automático de consultas** | não implementado — não existe telemetria de query, timestamp ou veredito persistida em arquivo. Toda consulta e seu resultado aparecem só no log de combate em tela (`CombatLogUI`), que não é salvo |
| **`GROUP BY`, `HAVING` e agregação** | não implementados. O jogo cobre `SELECT`, `FROM`, `WHERE`, `AND`/`OR`, operadores, `ORDER BY`, `LIMIT` e `JOIN` |
| **DDL e DML** | fora do escopo — o jogo é somente leitura |
| **Fases fixas** | os 5 andares são pré-definidos; não há geração procedural |
| **Modo colaborativo** | ausente — o jogo é estritamente single-player |
| **Plataforma** | Windows x64 apenas |

### Trabalhos futuros

1. Aplicar o protocolo de avaliação delineado no artigo
2. Implementar registro automático de consultas (query, timestamp, veredito),
   o que permitiria medir a evolução do erro **durante** a partida, não só no
   pós-teste
3. Incorporar mecânica de agregação (`GROUP BY` + `HAVING`) — o desenho já
   existe: fundir fragmentos do mesmo elemento exigiria
   `GROUP BY Elemento HAVING COUNT(*) >= 3` para liberar a magia de nível 3
4. Investigar retenção em prazo mais longo, com teste posterior ao encerramento
   da disciplina
5. Explorar uso multi-sessão e em grupo (moderadores de eficácia apontados por
   Wouters et al., 2013, que o formato atual — single-player, sessão única —
   não exercita)

---

## 18. Perguntas prováveis da banca

Um roteiro rápido de revisão — não são as únicas perguntas possíveis, mas cobrem
os pontos onde uma resposta vaga costuma custar pontos.

**"Por que não usar direto o SQLite / um SGBD qualquer?"**
Porque um SGBD executa silenciosamente uma consulta logicamente equivocada e
não sinaliza a falha (Taipalus et al., 2018). O interpretador próprio conhece
a *intenção* pedagógica da consulta e devolve uma mensagem específica sobre o
que está errado — não apenas "0 resultados".

**"O jogo é gamificação ou jogo educacional? Como você sabe a diferença?"**
Jogo educacional. O teste é: tire o elemento de jogo e veja se sobra a
atividade original. Numa lista de exercícios gamificada, tirando os pontos a
lista continua existindo. No QueryQuest, tirar o SQL não deixa nada — a magia
*é* a consulta.

**"Que erro o jogo mais ataca: sintático, semântico ou lógico?"**
Lógico. Erro sintático o SQLite já recusa sozinho (mensagem clara). O ponto
cego que a literatura documenta é o erro lógico: a consulta roda, devolve
algo, e nada avisa que está errado. É aí que o jogo intervém — convertendo
precisão lógica em consequência mecânica (dano errado, mana desperdiçada).

**"Como o custo de mana é calculado, exatamente?"**
Base 45 para toda consulta de magia. Desconta 10 por `WHERE`, 8 por cada
`AND`/`OR`, 12 por `ORDER BY`, 6 por `LIMIT`, 5 por operador de comparação, com
piso de 8. Consulta de análise (`FROM Inimigos`) segue regra diferente: paga 8
por coluna revelada, e `SELECT *` cobra pelas 7 colunas do inimigo.

**"O jogador vê esse custo antes de executar?"**
Sim — desde a última iteração da interface, uma barra na aba QUERY mostra o
custo caindo em tempo real conforme a consulta é digitada, usando a mesma
função (`ManaSystem.Prever`) que depois cobra de verdade.

**"Por que roguelite, e não fases lineares sem penalidade de reinício?"**
Perder e recomeçar obriga a reformular as mesmas consultas em contextos
ligeiramente diferentes, com baixo custo emocional (é esperado perder, faz
parte do formato). Isso corresponde a prática distribuída e prática de
recuperação — duas das técnicas de estudo com maior evidência de eficácia
(Dunlosky et al., 2013) — de forma incidental, sem o aluno precisar organizar
uma rotina de revisão por conta própria.

**"Por que o turno não passa automaticamente ao lançar a magia?"**
Para dissociar o tempo de *pensar* na consulta do tempo de *reagir* ao
combate. O jogador pode formular, testar e corrigir sem pressão de turno — só
o botão ENCERRAR TURNO avança o estado.

**"Como você garante que uma consulta com `'; DROP TABLE` não quebra o jogo?"**
Duas camadas. Primeiro, `BuildSafeQuery` nunca concatena string — monta SQL
parametrizado com placeholders, então mesmo um valor malicioso vira só um
literal de comparação, não código executável. Segundo, na tela de absorção,
`ValidarJoin` recusa explicitamente `;` e palavras-chave de DDL/DML antes de
chegar ao banco.

**"O trabalho testou o jogo com alunos de verdade?"**
Não. É desenvolvimento com proposta de avaliação: o artigo define um
protocolo replicável (pré-teste, intervenção, pós-teste, questionário Likert),
mas a aplicação empírica não foi conduzida dentro do escopo deste TCC — fica
como trabalho futuro, declarado explicitamente como limitação.

**"O jogo registra as consultas dos jogadores para análise?"**
Não automaticamente — é uma limitação real, não um recurso implementado. O que
existe é o log de combate em tela, que não é persistido. Registrar
consulta+timestamp+veredito em arquivo é listado como melhoria futura,
porque isso permitiria medir a evolução do erro *durante* a sessão de jogo.

**"Qual literatura fundamenta a escolha por consequência mecânica graduada, em vez de feedback binário (certo/errado)?"**
A lacuna identificada nas ferramentas existentes (SQL Island, SQLZOO,
Schemaverse, SQL Murder Mystery): todas avaliam a consulta como
funciona/não-funciona, sem distinguir uma consulta que apenas retorna o
resultado certo de uma consulta bem formulada. O QueryQuest responde a essa
lacuna especificamente, não à literatura de SQL education em geral.

**"Por que a UI é montada por código, e não em prefabs no editor?"**
Prefabs e cenas do Unity são arquivos binários/YAML complexos que o Git não
mescla bem — dois desenvolvedores mexendo na mesma tela geram conflito
irrecuperável. Construir a UI por código torna cada mudança de interface um
diff revisável, como qualquer outra mudança de lógica.

**"Cite um bug real que vocês encontraram e corrigiram, e o que ele ensina sobre o próprio tema do TCC."**
O da extração de valores com apóstrofo no `WHERE` (seção 15): a regex não
ancorada cortava `'Jato d'Agua'` em `"Jato d"` silenciosamente, sem erro na
tela — a consulta "funcionava" e devolvia o resultado errado. É, literalmente,
um erro lógico do tipo que o próprio SQLInterpreter foi criado para expor ao
jogador.

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
