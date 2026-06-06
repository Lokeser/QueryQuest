// Assets/_QueryQuest/Database/Scripts/DatabaseSeeder.cs
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
        }

        private static void SeedSpells(SQLiteConnection db)
        {
            if (db.Table<SpellData>().Count() > 0)
            {
                Debug.Log("[Seeder] Tabela Magias já populada. Pulando seed.");
                return;
            }

            var spells = new SpellData[]
            {
                // ─── FOGO ───────────────────────────────────────────────────
                new() { Nome = "Bola de Fogo",     Elemento = "Fogo",  Nivel = 1, Distancia = "MEDIO", DanoBase = 30, Desbloqueado = 1,
                        Descricao = "Uma esfera de chamas lançada contra o inimigo." },
                new() { Nome = "Meteoro",           Elemento = "Fogo",  Nivel = 3, Distancia = "LONGO", DanoBase = 80, Desbloqueado = 0,
                        Descricao = "Uma rocha incandescente convocada do céu." },
                new() { Nome = "Chama Proxima",     Elemento = "Fogo",  Nivel = 1, Distancia = "CURTO", DanoBase = 45, Desbloqueado = 1,
                        Descricao = "Fogo concentrado em combate corpo a corpo." },

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
                new() { Nome = "Tornado",            Elemento = "Vento", Nivel = 3, Distancia = "LONGO", DanoBase = 70, Desbloqueado = 0,
                        Descricao = "Um vórtice devastador." },

                // ─── TERRA ───────────────────────────────────────────────────
                new() { Nome = "Pua de Pedra",       Elemento = "Terra", Nivel = 1, Distancia = "CURTO", DanoBase = 40, Desbloqueado = 1,
                        Descricao = "Espigões de pedra erguem-se do solo." },
                new() { Nome = "Terremoto",          Elemento = "Terra", Nivel = 3, Distancia = "MEDIO", DanoBase = 65, Desbloqueado = 0,
                        Descricao = "O solo racha sob os pés do inimigo." },

                // ─── RAIO ─────────────────────────────────────────────────────
                new() { Nome = "Descarga Eletrica",  Elemento = "Raio",  Nivel = 1, Distancia = "CURTO", DanoBase = 35, Desbloqueado = 1,
                        Descricao = "Uma faísca elétrica dispara das mãos." },
                new() { Nome = "Tempestade",         Elemento = "Raio",  Nivel = 3, Distancia = "LONGO", DanoBase = 85, Desbloqueado = 0,
                        Descricao = "Raios múltiplos caem do céu sombrio." },
            };

            db.InsertAll(spells);
            Debug.Log($"[Seeder] {spells.Length} feitiços inseridos.");
        }

        private static void SeedEnemies(SQLiteConnection db)
        {
            if (db.Table<EnemyData>().Count() > 0)
            {
                Debug.Log("[Seeder] Tabela Inimigos já populada. Pulando seed.");
                return;
            }

            var enemies = new EnemyData[]
            {
                new() { Nome = "Registro Corrompido Alfa", Elemento = "Fogo",  HP = 60,  Nivel = 1, FraquezaElemento = "Agua",
                        Descricao = "Um dado corrompido que irradia calor." },
                new() { Nome = "Registro Corrompido Beta", Elemento = "Agua",  HP = 80,  Nivel = 1, FraquezaElemento = "Raio",
                        Descricao = "Uma entidade líquida e instável." },
                new() { Nome = "Registro Nulo",            Elemento = "Terra", HP = 120, Nivel = 2, FraquezaElemento = "Vento",
                        Descricao = "Um construto de pedra sem valor definido." },
                new() { Nome = "Overflow Elemental",       Elemento = "Raio",  HP = 100, Nivel = 2, FraquezaElemento = "Terra",
                        Descricao = "Energia elétrica transbordante." },
                new() { Nome = "Corruptor Primordial",     Elemento = "Vento", HP = 200, Nivel = 3, FraquezaElemento = "Fogo",
                        Descricao = "O chefe dos registros corrompidos." },
            };

            db.InsertAll(enemies);
            Debug.Log($"[Seeder] {enemies.Length} inimigos inseridos.");
        }
    }
}