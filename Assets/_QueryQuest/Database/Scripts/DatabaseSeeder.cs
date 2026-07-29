// Assets/_QueryQuest/Database/Scripts/DatabaseSeeder.cs
using System.Linq;
using SQLite;
using QueryQuest.Models;
using UnityEngine;

namespace QueryQuest.Database
{
    /// <summary>
    /// Popula as tabelas com dados iniciais caso estejam vazias.
    /// Chamado apenas uma vez na primeira inicialização.
    /// </summary>
    public static class DatabaseSeeder
    {
        public static void SeedIfEmpty(SQLiteConnection db)
        {
            SeedSpells(db);
            SeedEnemies(db);
            SeedFragments(db);
        }

        // ─────────────────────────────────────────────────────────────────────
        // FRAGMENTOS — 4 por golem, ligados por InimigoID (é o que o JOIN percorre)
        // ─────────────────────────────────────────────────────────────────────

        private static void SeedFragments(SQLiteConnection db)
        {
            if (db.Table<FragmentoData>().Count() > 0)
            {
                Debug.Log("[Seeder] Tabela Fragmentos já populada. Pulando seed.");
                return;
            }

            var fragments = new FragmentoData[]
            {
                // ── Golem de Fogo (1) — fraco a Agua, vulnerável a LONGO, golpe CURTO
                new() { FragmentoID = 1, InimigoID = 1, Nome = "Registro de Fogo",      Elemento = "Fogo",  Tipo = "Ofensivo",   Raridade = 2, ValorXP = 20,
                        Descricao = "Um registro que ainda queima ao ser lido.",        Pista = "Quem largou isto se apaga perto de agua corrente." },
                new() { FragmentoID = 2, InimigoID = 1, Nome = "Cinza Arcana",          Elemento = "Fogo",  Tipo = "Utilitario", Raridade = 1, ValorXP = 10,
                        Descricao = "O que sobra de um dado queimado ate o fim.",       Pista = "O dono disto so alcanca quem chega colado nele." },
                new() { FragmentoID = 3, InimigoID = 1, Nome = "Nucleo Incandescente",  Elemento = "Fogo",  Tipo = "Raro",       Raridade = 3, ValorXP = 35,
                        Descricao = "O coracao ardente da corrupcao.",                  Pista = "Golens assim detestam ser atingidos de muito longe." },
                new() { FragmentoID = 4, InimigoID = 1, Nome = "Essencia Flamejante",   Elemento = "Fogo",  Tipo = "Ofensivo",   Raridade = 4, ValorXP = 55,
                        Descricao = "Fogo puro comprimido em um unico dado.",           Pista = "A chama dele nunca alcancou nada a media distancia." },

                // ── Golem de Agua (2) — fraco a Raio, vulnerável a MEDIO, golpe CURTO
                new() { FragmentoID = 5, InimigoID = 2, Nome = "Gota de Dados",         Elemento = "Agua",  Tipo = "Utilitario", Raridade = 1, ValorXP = 10,
                        Descricao = "Uma gota que escorre e se reescreve.",             Pista = "O dono disto trava quando uma faisca o atravessa." },
                new() { FragmentoID = 6, InimigoID = 2, Nome = "Bolha Pura",            Elemento = "Agua",  Tipo = "Defensivo",  Raridade = 2, ValorXP = 20,
                        Descricao = "Uma bolha de dados sem nenhuma corrupcao.",        Pista = "Ele se defende mal quando o encaram a meia distancia." },
                new() { FragmentoID = 7, InimigoID = 2, Nome = "Cristal Gelado",        Elemento = "Agua",  Tipo = "Raro",       Raridade = 3, ValorXP = 35,
                        Descricao = "Agua que congelou no meio de uma consulta.",       Pista = "O golpe dele so pega quem esta ao seu lado." },
                new() { FragmentoID = 8, InimigoID = 2, Nome = "Essencia Torrencial",   Elemento = "Agua",  Tipo = "Ofensivo",   Raridade = 4, ValorXP = 55,
                        Descricao = "Uma enchente inteira guardada em um dado.",        Pista = "Nenhuma armadura dele resiste a energia eletrica." },

                // ── Golem de Terra (3) — fraco a Vento, vulnerável a CURTO, golpe MEDIO
                new() { FragmentoID = 9,  InimigoID = 3, Nome = "Seixo Corrompido",     Elemento = "Terra", Tipo = "Utilitario", Raridade = 1, ValorXP = 10,
                        Descricao = "Uma pedra pequena com um indice quebrado dentro.", Pista = "Lamina de vento corta esse tipo de pedra." },
                new() { FragmentoID = 10, InimigoID = 3, Nome = "Placa de Rocha",       Elemento = "Terra", Tipo = "Defensivo",  Raridade = 2, ValorXP = 20,
                        Descricao = "Parte da couraca de um construto de pedra.",       Pista = "A guarda dele falha contra quem luta colado." },
                new() { FragmentoID = 11, InimigoID = 3, Nome = "Geodo Ambarino",       Elemento = "Terra", Tipo = "Raro",       Raridade = 3, ValorXP = 35,
                        Descricao = "Cristais ambar crescidos dentro de dados antigos.",Pista = "O bracao dele alcanca a media distancia." },
                new() { FragmentoID = 12, InimigoID = 3, Nome = "Essencia Telurica",    Elemento = "Terra", Tipo = "Ofensivo",   Raridade = 4, ValorXP = 55,
                        Descricao = "O peso de uma montanha inteira em um dado.",       Pista = "Ele e' pesado demais para desviar de perto." },

                // ── Golem de Raio (4) — fraco a Terra, vulnerável a LONGO, golpe MEDIO
                new() { FragmentoID = 13, InimigoID = 4, Nome = "Faisca Perdida",       Elemento = "Raio",  Tipo = "Utilitario", Raridade = 1, ValorXP = 10,
                        Descricao = "Uma faisca que escapou de um indice queimado.",    Pista = "Terra firme aterra o dono disto." },
                new() { FragmentoID = 14, InimigoID = 4, Nome = "Bobina Estatica",      Elemento = "Raio",  Tipo = "Ofensivo",   Raridade = 2, ValorXP = 20,
                        Descricao = "Ainda solta choques quando manuseada.",            Pista = "Ele nao se protege do que vem de muito longe." },
                new() { FragmentoID = 15, InimigoID = 4, Nome = "Capacitor Runico",     Elemento = "Raio",  Tipo = "Raro",       Raridade = 3, ValorXP = 35,
                        Descricao = "Guarda uma carga que nunca descarrega.",           Pista = "O raio dele viaja bem ate a media distancia." },
                new() { FragmentoID = 16, InimigoID = 4, Nome = "Essencia Fulminante",  Elemento = "Raio",  Tipo = "Ofensivo",   Raridade = 4, ValorXP = 55,
                        Descricao = "Uma tempestade eletrica em estado bruto.",         Pista = "Rochas e po sao o fim de qualquer corrente." },

                // ── Golem Primordial (5, boss) — fraco a Fogo, vulnerável a CURTO, golpe LONGO
                new() { FragmentoID = 17, InimigoID = 5, Nome = "Sopro Ancestral",      Elemento = "Vento", Tipo = "Defensivo",  Raridade = 2, ValorXP = 20,
                        Descricao = "O primeiro registro de ar que o sistema guardou.", Pista = "Ate o ancestral recua diante do fogo." },
                new() { FragmentoID = 18, InimigoID = 5, Nome = "Lamina de Vento",      Elemento = "Vento", Tipo = "Ofensivo",   Raridade = 3, ValorXP = 35,
                        Descricao = "Corta consultas ao meio antes de executarem.",     Pista = "Ele acerta de longe, mas se perde no corpo a corpo." },
                new() { FragmentoID = 19, InimigoID = 5, Nome = "Olho da Tempestade",   Elemento = "Vento", Tipo = "Raro",       Raridade = 4, ValorXP = 55,
                        Descricao = "O centro imovel de uma corrupcao gigante.",        Pista = "Quem chega perto dele encontra a guarda aberta." },
                new() { FragmentoID = 20, InimigoID = 5, Nome = "Essencia Primordial",  Elemento = "Vento", Tipo = "Raro",       Raridade = 5, ValorXP = 90,
                        Descricao = "A origem de toda a corrupcao dos registros.",      Pista = "A chama e a unica coisa que o primordial nunca copiou." },
            };

            db.InsertAll(fragments);
            Debug.Log($"[Seeder] {fragments.Length} fragmentos inseridos.");
        }

        private static void SeedSpells(SQLiteConnection db)
        {
            var spells = new SpellData[]
            {
                // ─── UTILITÁRIO ──────────────────────────────────────────────
                new() { Nome = "Analise",          Elemento = "Neutro", Nivel = 1, Distancia = "MEDIO", DanoBase = 0, Desbloqueado = 1,
                        Descricao = "Revela informações do inimigo. Use SELECT para escolher o que descobrir (Elemento, Ataque, Nivel...)." },
                new() { Nome = "Inspecionar Fragmento", Elemento = "Neutro", Nivel = 3, Distancia = "MEDIO", DanoBase = 0, Desbloqueado = 1,
                        Descricao = "Abre o inventário de fragmentos. Usa JOIN para cruzar Fragmentos com Inimigos e revelar de quem veio cada essência." },

                // ─── FOGO ───────────────────────────────────────────────────
                new() { Nome = "Bola de Fogo",     Elemento = "Fogo",  Nivel = 1, Distancia = "MEDIO", DanoBase = 30, Desbloqueado = 1,
                        Descricao = "Uma esfera de chamas lançada contra o inimigo." },
                new() { Nome = "Meteoro",           Elemento = "Fogo",  Nivel = 3, Distancia = "LONGO", DanoBase = 80, Desbloqueado = 0,
                        Descricao = "Uma rocha incandescente convocada do céu." },
                new() { Nome = "Chama Proxima",     Elemento = "Fogo",  Nivel = 1, Distancia = "CURTO", DanoBase = 45, Desbloqueado = 1,
                        Descricao = "Fogo concentrado em combate corpo a corpo." },
                new() { Nome = "Lanca Flamejante",  Elemento = "Fogo",  Nivel = 2, Distancia = "MEDIO", DanoBase = 48, Desbloqueado = 0,
                        Descricao = "Absorva um fragmento de Fogo para dominar esta magia." },

                // ─── ÁGUA ────────────────────────────────────────────────────
                new() { Nome = "Jato d'Agua",       Elemento = "Agua",  Nivel = 1, Distancia = "MEDIO", DanoBase = 28, Desbloqueado = 1,
                        Descricao = "Um poderoso jato de água pressurizada." },
                new() { Nome = "Tsunami",            Elemento = "Agua",  Nivel = 3, Distancia = "LONGO", DanoBase = 75, Desbloqueado = 0,
                        Descricao = "Uma onda colossal que varre tudo." },
                new() { Nome = "Gelo Afiado",        Elemento = "Agua",  Nivel = 2, Distancia = "CURTO", DanoBase = 50, Desbloqueado = 0,
                        Descricao = "Cristais de gelo convocados à queima-roupa." },

                // ─── VENTO ───────────────────────────────────────────────────
                new() { Nome = "Rajada",             Elemento = "Vento", Nivel = 1, Distancia = "MEDIO", DanoBase = 25, Desbloqueado = 1,
                        Descricao = "Um corte de vento preciso." },
                new() { Nome = "Vendaval Cortante",  Elemento = "Vento", Nivel = 2, Distancia = "MEDIO", DanoBase = 44, Desbloqueado = 0,
                        Descricao = "Absorva um fragmento de Vento para dominar esta magia." },
                new() { Nome = "Tornado",            Elemento = "Vento", Nivel = 3, Distancia = "LONGO", DanoBase = 70, Desbloqueado = 0,
                        Descricao = "Um vórtice devastador." },

                // ─── TERRA ───────────────────────────────────────────────────
                new() { Nome = "Pua de Pedra",       Elemento = "Terra", Nivel = 1, Distancia = "CURTO", DanoBase = 40, Desbloqueado = 1,
                        Descricao = "Espigões de pedra erguem-se do solo." },
                new() { Nome = "Lanca de Granito",   Elemento = "Terra", Nivel = 2, Distancia = "MEDIO", DanoBase = 46, Desbloqueado = 0,
                        Descricao = "Absorva um fragmento de Terra para dominar esta magia." },
                new() { Nome = "Terremoto",          Elemento = "Terra", Nivel = 3, Distancia = "MEDIO", DanoBase = 65, Desbloqueado = 0,
                        Descricao = "O solo racha sob os pés do inimigo." },

                // ─── RAIO ─────────────────────────────────────────────────────
                new() { Nome = "Descarga Eletrica",  Elemento = "Raio",  Nivel = 1, Distancia = "CURTO", DanoBase = 35, Desbloqueado = 1,
                        Descricao = "Uma faísca elétrica dispara das mãos." },
                new() { Nome = "Relampago Guiado",   Elemento = "Raio",  Nivel = 2, Distancia = "MEDIO", DanoBase = 45, Desbloqueado = 0,
                        Descricao = "Absorva um fragmento de Raio para dominar esta magia." },
                new() { Nome = "Tempestade",         Elemento = "Raio",  Nivel = 3, Distancia = "LONGO", DanoBase = 85, Desbloqueado = 0,
                        Descricao = "Raios múltiplos caem do céu sombrio." },
            };

            if (db.Table<SpellData>().Count() == 0)
            {
                db.InsertAll(spells);
                Debug.Log($"[Seeder] {spells.Length} feitiços inseridos.");
                return;
            }

            // Banco de uma versão anterior: insere só as magias que ainda não
            // existem (as de nível 2 e as utilitárias novas). Sem isto, quem já
            // tinha o .db nunca receberia as magias absorvíveis por JOIN.
            int novas = 0;
            foreach (var s in spells)
            {
                var existente = db.Query<SpellData>("SELECT * FROM Magias WHERE Nome = ?", s.Nome);
                if (existente == null || existente.Count == 0)
                {
                    db.Insert(s);
                    novas++;
                }
            }

            if (novas > 0) Debug.Log($"[Seeder] {novas} magias novas adicionadas ao banco existente.");
            else Debug.Log("[Seeder] Tabela Magias já atualizada.");
        }

        private static void SeedEnemies(SQLiteConnection db)
        {
            var existing = db.Table<EnemyData>().ToList();

            // Migração: bancos criados antes da troca para os Golens ainda têm os
            // nomes antigos ("Registro Corrompido...") — limpa e reinsere.
            bool outdated = existing.Count > 0 &&
                            existing.Exists(e => e.Nome == null || !e.Nome.StartsWith("Golem"));

            if (existing.Count > 0 && !outdated)
            {
                Debug.Log("[Seeder] Tabela Inimigos já populada. Pulando seed.");
                return;
            }

            if (outdated)
            {
                db.DeleteAll<EnemyData>();
                // Zera o contador do AUTOINCREMENT: o FloorManager mapeia andar 1..5
                // → inimigo Id 1..5, então os golens precisam renascer com Ids 1..5.
                db.Execute("DELETE FROM sqlite_sequence WHERE name = 'Inimigos'");
                Debug.Log("[Seeder] Inimigos antigos detectados — migrando para os Golens.");
            }

            // Ordem de inserção = Id 1..5 = ordem dos andares (FloorManager)
            var enemies = new EnemyData[]
            {
                new() { Nome = "Golem de Fogo",    Elemento = "Fogo",  HP = 60,  Nivel = 1, FraquezaElemento = "Agua",  AtaqueDistancia = "CURTO", FraquezaDistancia = "LONGO",
                        Descricao = "Um golem de cristais flamejantes que irradia calor." },
                new() { Nome = "Golem de Agua",    Elemento = "Agua",  HP = 80,  Nivel = 1, FraquezaElemento = "Raio",  AtaqueDistancia = "CURTO", FraquezaDistancia = "MEDIO",
                        Descricao = "Um golem de cristais aquosos, fluido e instável." },
                new() { Nome = "Golem de Terra",   Elemento = "Terra", HP = 120, Nivel = 2, FraquezaElemento = "Vento", AtaqueDistancia = "MEDIO", FraquezaDistancia = "CURTO",
                        Descricao = "Um golem de rocha maciça com cristais de âmbar." },
                new() { Nome = "Golem de Raio",    Elemento = "Raio",  HP = 100, Nivel = 2, FraquezaElemento = "Terra", AtaqueDistancia = "MEDIO", FraquezaDistancia = "LONGO",
                        Descricao = "Um golem de cristais violeta carregados de energia." },
                new() { Nome = "Golem Primordial", Elemento = "Vento", HP = 200, Nivel = 3, FraquezaElemento = "Fogo",  AtaqueDistancia = "LONGO", FraquezaDistancia = "CURTO",
                        Descricao = "O golem ancestral de cristais esmeralda, chefe dos golens." },
            };

            db.InsertAll(enemies);
            Debug.Log($"[Seeder] {enemies.Length} inimigos inseridos.");
        }
    }
}
