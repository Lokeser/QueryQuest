# Gerando a build do QueryQuest e levando para outro computador

Guia para gerar o executável Windows e rodá-lo em outra máquina — sem repetir os
erros da primeira tentativa.

---

## Antes de tudo: por que a build anterior falhou

Os quatro problemas relatados **eram um só**, em cascata. Vale entender, porque
isso muda o que você procura quando algo der errado:

```
sqlite3.dll não foi incluído na build
        ↓
new SQLiteConnection(...) lança DllNotFoundException
        ↓
DatabaseManager captura o erro e deixa DB = null
        ↓
FloorManager fica preso em WaitUntil(DB != null) — para sempre
        ↓
o combate NUNCA começa, e a partir daí:
        · estado nunca vira PLAYER_TURN  →  botões de movimento ficam desabilitados
        · nenhuma consulta ao banco       →  lista de magias vazia
        · StartCombat nunca roda          →  barras de vida e mana sem valores
```

Ou seja: **botões travados, magias sumidas e vida zerada eram sintomas, não
defeitos separados.** Resolvido o banco, os três desaparecem juntos.

A causa raiz: **a sqlite3.dll do projeto era um binário ARM64**, não x86_64 —
arquitetura errada para qualquer PC comum. No editor do desktop o jogo
funcionava por acidente: quando o plugin não carrega, o Mono procura uma
`sqlite3.dll` no PATH do sistema e encontrava a do miniconda (x64). No notebook
não há miniconda, o fallback não existe, e o banco morria na abertura.

**Isso já foi corrigido no repositório.** As correções estão versionadas:

| Arquivo | Para que serve |
|---|---|
| `Assets/Plugins/SQLite/x86_64/sqlite3.dll` | substituída por uma SQLite 3.45.3 x86_64 de verdade (verificada no cabeçalho PE e testada isolada, sem PATH) |
| `Assets/Plugins/SQLite/x86_64/sqlite3.dll.meta` | plugin nativo Windows x86_64, habilitado para Editor e Standalone Win64 |
| `Assets/link.xml` | impede o *managed stripping* de remover os tipos que o sqlite-net usa por reflexão |

---

## 1. Conferir o projeto antes de compilar

Abra o projeto no Unity e verifique:

**1.1. O plugin nativo está configurado**

No Project, selecione `Assets/Plugins/SQLite/x86_64/sqlite3.dll`. No Inspector,
a aba **Standalone** precisa estar marcada, com **CPU = x86_64** e **OS = Windows**.
Se as caixas aparecerem desmarcadas, marque e clique em **Apply**.

> Este é o item que quebrou a build anterior. Não pule.

**1.2. As cenas estão no build, na ordem certa**

`File → Build Profiles` (ou `Build Settings`). Devem constar, nesta ordem:

```
0  Assets/Scenes/MainMenu.unity     ← precisa ser a primeira
1  Assets/Scenes/SampleScene.unity
```

Se a ordem estiver invertida, o jogo abre direto no combate e pula o menu.

**1.3. O banco não está com progresso de teste**

O arquivo `Assets/StreamingAssets/queryquest.db` vai junto na build e vira o
estado inicial de quem jogar. Se você jogou no editor, ele guarda magias já
desbloqueadas.

Para restaurar o estado de início de jogo, rode na raiz do projeto:

```bash
python -c "import sqlite3; c=sqlite3.connect('Assets/StreamingAssets/queryquest.db'); c.execute('UPDATE Magias SET Desbloqueado=0 WHERE Nivel>=2 AND Elemento<>\"Neutro\"'); c.execute('UPDATE Magias SET Desbloqueado=1 WHERE Nivel=1 OR Elemento=\"Neutro\"'); c.commit(); print('desbloqueadas:', c.execute('SELECT COUNT(*) FROM Magias WHERE Desbloqueado=1').fetchone()[0])"
```

O resultado esperado é **8** magias desbloqueadas: as 7 de nível 1 mais
*Inspecionar Fragmento*, que é utilitária e começa liberada.

**1.4. Nenhum erro no Console**

Se houver erro de compilação, o Unity gera a build com scripts desatualizados —
ou nem gera. O Console precisa estar limpo antes de continuar.

---

## 2. Gerar a build

1. `File → Build Profiles`
2. Plataforma: **Windows** · Arquitetura: **x86_64**
3. **Development Build**: deixe **marcado** nesta primeira build para o notebook.
   Ele habilita o log detalhado, que é o que permite diagnosticar se algo falhar.
   Depois de confirmar que roda, gere uma build final sem essa opção.
4. Clique em **Build**
5. Escolha uma pasta **vazia**, fora de `Assets/`. Sugestão: `Build/Windows/`
   na raiz do projeto (já está no `.gitignore` por não ser código-fonte).

O resultado é algo assim:

```
Build/Windows/
├── QueryQuest_New.exe        <- o nome vem de Product Name nas Player Settings
├── UnityPlayer.dll
├── UnityCrashHandler64.exe
└── QueryQuest_New_Data/
    ├── StreamingAssets/
    │   └── queryquest.db      ← confira que este arquivo existe
    ├── Plugins/
    │   └── x86_64/
    │       └── sqlite3.dll    ← e este também
    ├── Managed/
    └── ...
```

### Verificação obrigatória antes de copiar

Confira que os dois arquivos abaixo existem na pasta gerada:

```bash
ls "Build/Windows/QueryQuest_New_Data/Plugins/x86_64/sqlite3.dll"
ls "Build/Windows/QueryQuest_New_Data/StreamingAssets/queryquest.db"
```

**Se `sqlite3.dll` não estiver lá, a build vai falhar exatamente como antes.**
Volte ao passo 1.1.

---

## 3. Testar na sua própria máquina primeiro

Antes de levar para o notebook, rode o `.exe` aqui. Mas com um cuidado:

O jogo copia o banco para a pasta de dados do usuário na primeira execução, e
**essa cópia sobrevive entre builds**. Se você já rodou alguma versão, apague:

```
C:\Users\<seu-usuario>\AppData\LocalLow\DefaultCompany\QueryQuest_New\queryquest.db
```

Sem isso você testa com o banco antigo e não descobre se a cópia inicial
funciona — que é justamente o caminho que roda no notebook.

Checklist do que precisa acontecer:

- [ ] o menu abre e os botões respondem ao clique
- [ ] NOVO JOGO entra no combate e o tutorial aparece
- [ ] as barras de HP e MP mostram 100/100
- [ ] os botões Recuar / Manter / Avançar ficam clicáveis no seu turno
- [ ] o grimório abre e a aba MAGIAS lista as magias
- [ ] uma consulta como `SELECT * FROM Magias WHERE Elemento = 'Fogo'` lança uma magia

Se qualquer um falhar, vá para a seção 5 antes de copiar.

---

## 4. Levar para o notebook

**Copie a pasta inteira.** O `.exe` sozinho não funciona — ele depende de
`QueryQuest_New_Data/` e `UnityPlayer.dll` ao lado.

```
Build/Windows/     ← copie esta pasta com todo o conteúdo
```

Vale compactar em `.zip` antes de transferir por pendrive ou nuvem: além de ser
mais rápido, evita que algum arquivo se perca no caminho.

No notebook:

1. Descompacte em uma pasta com **permissão de escrita** (Documentos ou Desktop).
   Evite `C:\Arquivos de Programas` — o jogo grava o banco em AppData, mas o
   Windows costuma criar restrições extras ali.
2. Rode `QueryQuest_New.exe`
3. Se o **SmartScreen** aparecer ("O Windows protegeu o computador"), clique em
   **Mais informações → Executar assim mesmo**. É esperado: o executável não tem
   assinatura digital.

### Requisitos do notebook

- Windows 64 bits
- **Visual C++ Redistributable 2015-2022 (x64)** — o `sqlite3.dll` depende dele.
  A maioria das máquinas já tem, mas se o jogo abrir e travar na tela inicial,
  instale: <https://aka.ms/vs/17/release/vc_redist.x64.exe>

Não precisa de Unity, nem de .NET, nem de internet: o banco é local e o jogo é
inteiramente offline.

---

## 5. Se algo der errado: leia o log

Este é o passo que economiza mais tempo. A build guarda um log completo em:

```
C:\Users\<usuario>\AppData\LocalLow\DefaultCompany\QueryQuest_New\Player.log
```

Abra o arquivo e procure por `[DatabaseManager]` e por `Exception`.

### Sintomas e causas

| O que você vê | Onde olhar | Causa provável |
|---|---|---|
| `DllNotFoundException: sqlite3` | Player.log | `sqlite3.dll` não veio na build → passo 1.1 |
| `[DatabaseManager] Falha ao inicializar banco` | Player.log | mesma coisa, ou VC++ Redistributable ausente |
| Botões travados **e** vida zerada **e** magias vazias | — | é a cascata do banco. **Não são três bugs** — procure o erro de SQLite no log |
| Só as magias não aparecem, resto funciona | Player.log | stripping removeu os modelos → confira que `Assets/link.xml` está no projeto |
| Menu abre mas nenhum botão clica | — | EventSystem ausente. O `MainMenuUI` cria um em runtime, então isso não deve ocorrer; se ocorrer, é erro de script antes disso — veja o log |
| Jogo abre direto no combate, sem menu | Build Profiles | ordem das cenas invertida → passo 1.2 |
| Tela preta ao abrir | Player.log | cena 0 ausente da lista de build |

### O teste decisivo

Se a vida, os botões e as magias falharem **ao mesmo tempo**, não investigue os
três. Procure no `Player.log` por:

```
[DatabaseManager] Banco copiado para persistentDataPath.
```

Se essa linha **não** existir, o banco não abriu — e é aí que está o problema.
Todo o resto é consequência.

---

## 6. Opcional: nome do executavel

Hoje o executavel sai como **QueryQuest_New.exe** e o log vai para
`AppData\LocalLow\DefaultCompany\QueryQuest_New\` — nomes herdados de quando o
projeto foi criado. Para a entrega, vale ajustar em
`Edit -> Project Settings -> Player`:

- **Company Name**: `UGB` (ou seu nome)
- **Product Name**: `QueryQuest`

Faca isso **antes** de gerar a build final, porque muda o nome do `.exe` e
tambem o caminho do banco e do log. Se mudar depois de ja ter testado, o jogo
cria um banco novo do zero no caminho novo — o que ate e desejavel para a
entrega, mas confunde se voce nao estiver esperando.

---

## 7. Build final para entrega

Depois de confirmar que tudo roda no notebook, gere a versão de entrega:

- **Development Build desmarcado** (menor e sem overlay de debug)
- pasta limpa, separada da build de teste
- teste rápido de novo, porque a build sem development usa outro caminho de
  compilação e, embora raro, pode se comportar de forma diferente

Guarde o `.zip` final junto com o artigo — se a banca pedir para ver o jogo, ele
está pronto para rodar em qualquer Windows 64 bits.
