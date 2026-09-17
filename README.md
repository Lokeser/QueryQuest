⚔️ QueryQuest

🧙 Aprenda SQL jogando.

No QueryQuest, consultas SQL não são apenas exercícios: elas são a sua magia.

🎮 O que é o QueryQuest?

QueryQuest é um jogo educacional de combate por turnos com estrutura roguelite, desenvolvido em Unity 6, no qual o jogador utiliza consultas SQL reais para interagir com o mundo do jogo.

Em vez de simplesmente clicar em um botão de ataque:

🧑‍💻 Você escreve uma consulta SQL
              ↓
🔎 O QueryQuest interpreta a consulta
              ↓
🗄️ A consulta é executada no SQLite
              ↓
✨ O resultado se transforma em uma ação
              ↓
⚔️ A ação afeta o combate

💡 A consulta não está ao lado da mecânica de jogo. A consulta é a mecânica de jogo.

🧠 A proposta educacional

Aprender SQL envolve muito mais do que memorizar comandos.

O jogador precisa:

entender o problema;

compreender os dados disponíveis;

formular uma consulta;

testar sua hipótese;

interpretar o resultado;

corrigir erros;

aplicar o conhecimento novamente em outro contexto.

O QueryQuest transforma esse ciclo em uma experiência interativa:

📚 CONCEITO
     ↓
🎯 DESAFIO
     ↓
⌨️ CONSULTA
     ↓
🔎 RESULTADO
     ↓
⚔️ CONSEQUÊNCIA
     ↓
💬 FEEDBACK
     ↓
📈 PROGRESSÃO
     ↓
🔄 NOVO DESAFIO

🎓 O objetivo

Explorar o uso de um jogo educacional como ambiente de prática para conceitos fundamentais de SQL, integrando conteúdo acadêmico, tomada de decisão, feedback e progressão.

🌎 O mundo do QueryQuest

O jogador atravessa 5 andares, enfrentando golems elementais e descobrindo informações sobre eles através do banco de dados.

🔥 Andar

👹 Inimigo

🌈 Elemento

❤️ HP

💥 Fraqueza

1

Golem de Fogo

🔥 Fogo

60

💧 Água

2

Golem de Água

💧 Água

80

⚡ Raio

3

Golem de Terra

🪨 Terra

120

🌪️ Vento

4

Golem de Raio

⚡ Raio

100

🪨 Terra

5

Golem Primordial

🌪️ Vento

200

🔥 Fogo

⚠️ A informação não é simplesmente entregue ao jogador.

Para descobrir a fraqueza de um inimigo, o jogador pode precisar consultar a tabela Inimigos.

Assim, o banco deixa de ser apenas armazenamento de dados e passa a fazer parte da própria experiência.

⚔️ Sistema de combate

O combate acontece por turnos em uma arena com 6 posições:

[1] [2] [3] [4] [5] [6]
 🧙                       👹

O jogador pode:

⬅️ recuar;

➖ permanecer;

➡️ avançar;

✨ lançar uma magia;

⏭️ encerrar o turno.

A distância influencia diretamente a efetividade das magias.

💥 Dano

Dano =
DanoBase
× MultiplicadorElemental
× EficiênciaDeDistância

O ciclo elemental é:

💧 Água → 🔥 Fogo → 🌪️ Vento → 🪨 Terra → ⚡ Raio → 💧 Água

Isso cria uma segunda camada de decisão:

🧠 Não basta descobrir qual magia usar. É preciso descobrir quando e de onde utilizá-la.

🧙 Grimório

O Grimório é o principal centro de interação do jogador.

🔮 QUERY

Terminal onde o jogador escreve suas consultas SQL.

Possui:

⌨️ editor de consulta;

💡 autocompletar;

💧 barra de custo de mana;

▶️ execução da consulta.

✨ MAGIAS

Exibe:

elemento;

nível;

alcance;

dano;

estado de desbloqueio.

🗄️ TABELAS

Apresenta o esquema do banco utilizado pelo jogo.

💡 A documentação do banco é obtida a partir do próprio SQLite, reduzindo a possibilidade de a documentação ficar diferente do esquema real.

🧑‍💻 SQL como mecânica

O QueryQuest trabalha atualmente com um subconjunto controlado de SQL.

✅ Implementado

SELECT
FROM
WHERE
AND
OR
=
!=
>
<
>=
<=
LIKE
ORDER BY
ASC
DESC
LIMIT
JOIN ... ON

🚧 Ainda não implementado

GROUP BY
HAVING
COUNT()
SUM()
AVG()
MIN()
MAX()

INSERT
UPDATE
DELETE
DDL

O jogo atualmente trabalha com consultas de leitura.

🔎 O Interpretador SQL

As consultas não são enviadas diretamente ao SQLite.

O QueryQuest possui um interpretador próprio:

⌨️ Consulta
   ↓
🧩 Análise sintática
   ↓
🔍 Validação semântica
   ↓
🛡️ Construção parametrizada
   ↓
🗄️ SQLite
   ↓
📊 Resultado
   ↓
🎮 Ação no jogo

Isso permite que o jogo ofereça respostas específicas para situações pedagógicas.

🛡️ Segurança

As consultas são construídas utilizando parâmetros, evitando concatenação direta de valores.

🔐 Mesmo sendo um banco local, o projeto evita ensinar uma prática vulnerável como parte da própria experiência de aprendizagem.

💧 Mana: quando SQL vira estratégia

Uma das mecânicas centrais do QueryQuest é transformar a precisão da consulta em uma decisão econômica.

Consultas de magia possuem custo base de 45 de mana.

🧩 Recurso

💧 Desconto

WHERE

−10

AND / OR adicional

−8

ORDER BY

−12

LIMIT

−6

Operador de comparação

−5

🔒 Piso mínimo

8

Por exemplo:

SELECT * FROM Magias

é uma consulta válida, mas pouco refinada.

Enquanto:

SELECT Nome
FROM Magias
WHERE Elemento = 'Fogo'
ORDER BY DanoBase DESC
LIMIT 1

é mais específica.

👁️ E o jogador vê isso acontecendo

A barra de mana é atualizada enquanto a consulta é digitada.

SELECT * FROM Magias
████████████████████ 45

        ↓ WHERE

███████████████░░░░░ 35

        ↓ ORDER BY

██████████░░░░░░░░░░ 23

        ↓ LIMIT

████░░░░░░░░░░░░░░░░ 17

🎯 A precisão da consulta deixa de ser apenas uma questão abstrata de estilo e passa a ter uma consequência perceptível dentro do jogo.

🧩 JOIN como progressão

O JOIN não aparece apenas como conteúdo teórico.

Ele é necessário para uma mecânica de progressão.

Ao derrotar um golem, o jogador pode encontrar um fragmento.

Para absorvê-lo e desbloquear uma magia de nível 2, precisa relacionar as tabelas:

SELECT f.Nome, i.Nome AS Inimigo
FROM Fragmentos f
JOIN Inimigos i ON f.InimigoID = i.Id
WHERE f.FragmentoID = 3

👹 Derrotar inimigo
        ↓
🧩 Encontrar fragmento
        ↓
🗄️ Relacionar tabelas
        ↓
🔗 JOIN
        ↓
✨ Desbloquear magia

💡 Sistema de dicas

O jogador pode solicitar ajuda em etapas:

🟢 descobrir que existem duas tabelas;

🟡 descobrir a coluna que conecta as tabelas;

🔴 receber um exemplo completo.

As dicas consomem mana, transformando a ajuda em um recurso estratégico, em vez de simplesmente entregar a resposta gratuitamente.

🎲 Estrutura Roguelite

A campanha possui progressão por runs.

Entre os andares, o jogador recebe 3 recompensas e escolhe uma:

🎁 Recompensa

⚙️ Efeito

🛡️ Armadura

reduz dano recebido

🪄 Cajado elemental

aumenta dano de um elemento

📖 Página de magia

desbloqueia uma magia

Ao perder:

💀 DERROTA
   ↓
🔄 NOVA RUN
   ↓
🧠 NOVA TENTATIVA

A estrutura permite que conceitos já apresentados reapareçam em contextos diferentes.

🔄 A ideia não é simplesmente repetir o mesmo exercício. É reutilizar o conhecimento em novas situações.

Após completar os cinco andares, o jogador pode acessar o:

♾️ Modo Infinito

Os inimigos são escalados e a build é preservada.

📚 Sistemas de apoio à aprendizagem

O jogo possui diversas ferramentas para reduzir barreiras sem simplesmente entregar a resposta.

Sistema

Função

🌙 Espírito da Lua

dicas leves sobre o inimigo

📖 Tutorial

apresenta os fundamentos

📜 Log de combate

registra os acontecimentos

💡 Autocompletar

sugere tabelas, colunas e cláusulas

💧 Barra de mana

mostra o custo da consulta em tempo real

🗄️ Documentação

apresenta o esquema do banco

✨ VFX

reforça visualmente as ações

🎞️ Animações

dão feedback visual às ações

🏗️ Arquitetura

O projeto possui aproximadamente 11.700 linhas de C# próprio, distribuídas em 66 scripts, além da biblioteca utilizada para SQLite.

Assets/_QueryQuest/
│
├── 📦 Models/
│   ├── SpellData
│   ├── EnemyData
│   └── FragmentoData
│
├── 🗄️ Database/
│   ├── DatabaseManager
│   ├── DatabaseSeeder
│   └── SQLInterpreter
│
├── ⚔️ Combat/
│   ├── CombatManager
│   ├── SlotSystem
│   ├── DamageCalculator
│   └── ...
│
└── 🖥️ UI/
    ├── HudSkin
    ├── GrimoireUI
    ├── QueryPanelSkin
    ├── FragmentosUI
    └── ...

🗄️ Banco de dados

O QueryQuest utiliza SQLite embarcado.

Principais tabelas:

✨ Magias

Informações das magias:

Id
Nome
Elemento
Nivel
Distancia
DanoBase
Descricao
Desbloqueado

👹 Inimigos

Informações dos adversários:

Id
Nome
Elemento
HP
Nivel
FraquezaElemento
AtaqueDistancia
FraquezaDistancia

🧩 Fragmentos

Informações dos fragmentos:

FragmentoID
InimigoID
Nome
Elemento
Tipo
Raridade
Descricao
ValorXP
Pista

O relacionamento:

Fragmentos.InimigoID
          │
          ▼
      Inimigos.Id

é utilizado diretamente pela mecânica de JOIN.

🧪 Validação

O projeto possui validações automatizadas executadas em Play Mode, verificando o comportamento real dos sistemas.

Entre os aspectos testados:

🔎 interpretação de consultas;

🧩 validação de JOIN;

💧 consumo de mana;

📊 progressão da barra de custo;

🖥️ comportamento da interface;

📐 posicionamento dos elementos;

🖥️ adaptação a diferentes resoluções;

📝 consultas contendo nomes com apóstrofo.

⚠️ Essas verificações são testes de execução do jogo e não devem ser confundidas com testes unitários tradicionais.

🚧 Limitações atuais

O projeto ainda está em desenvolvimento.

Limitação

Estado

🎓 Avaliação empírica com estudantes

Não conduzida

📊 Telemetria automática de consultas

Não implementada

GROUP BY / HAVING / agregações

Não implementados

🛠️ DDL / DML

Fora do escopo atual

🗺️ Geração procedural dos andares

Não implementada

👥 Multiplayer

Não implementado

🖥️ Plataforma

Windows x64

🚀 Próximos passos

Entre as possibilidades de evolução:

🧪 aplicar o protocolo de avaliação com estudantes;

📊 registrar consultas e resultados para análise;

🧮 implementar GROUP BY, HAVING e funções de agregação;

🧠 investigar retenção do conhecimento em prazo mais longo;

🗺️ ampliar a campanha;

👹 adicionar novos inimigos e desafios;

🎁 ampliar sistemas de recompensa;

🔀 aumentar a variedade de situações SQL;

👥 explorar experiências colaborativas.

🧠 A ideia central

O QueryQuest busca transformar:

📚 SQL
+
🎮 Game Design
+
🧠 Aprendizagem

em uma única experiência.

O objetivo não é simplesmente colocar perguntas de SQL dentro de um jogo.

É fazer com que:

SQL → seja a ação
Ação → produza uma consequência
Consequência → gere feedback
Feedback → permita adaptação
Adaptação → produza progressão
Progressão → incentive nova prática

⚔️ No QueryQuest, aprender SQL faz parte de jogar.

🛠️ Tecnologias

🎮 Unity 6 — 6000.3.10f1

💻 C#

🗄️ SQLite

🔌 sqlite-net

📝 TextMeshPro

🖱️ Unity UI / Input

🌿 Git

📂 Documentação

Para uma descrição técnica aprofundada do projeto, consulte:

📘 DOCUMENTACAO.md

O documento contém:

arquitetura detalhada;

funcionamento dos sistemas;

banco de dados;

interpretador SQL;

combate;

economia de mana;

progressão;

decisões técnicas;

bugs encontrados e corrigidos;

validação;

limitações;

perguntas prováveis da banca.

🎓 Projeto acadêmico

QueryQuest
Jogo educacional para prática de SQL

👨‍💻 Autor: Guilherme Muniz Narciso
🎓 Curso: Sistemas de Informação
🏫 Instituição: Centro Universitário Geraldo Di Biase — UGB/FERP
👨‍🏫 Orientador: Fábio dos Santos Gonçalves

<div align="center">

✨ QueryQuest

Query. Learn. Fight. Repeat.

SQL × RPG × Roguelite × Educação

</div>
