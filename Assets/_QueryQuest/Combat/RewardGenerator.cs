// Assets/_QueryQuest/Combat/RewardGenerator.cs
// Gera 3 itens de recompensa aleatórios após vencer um andar.

using System.Collections.Generic;
using System.Linq;
using SQLite;
using UnityEngine;
using QueryQuest.Database;
using QueryQuest.Models;

namespace QueryQuest.Combat
{
    public static class RewardGenerator
    {
        private static readonly string[] Elements = { "Fogo", "Agua", "Vento", "Terra", "Raio" };

        /// <summary>
        /// Gera 3 itens aleatórios. Tipos são sorteados independentemente,
        /// então pode vir 2 armaduras + 1 cajado, por exemplo.
        /// </summary>
        public static List<RewardItem> Generate(int count = 3)
        {
            var items = new List<RewardItem>();
            var db = DatabaseManager.Instance?.DB;

            // Busca magias ainda bloqueadas para oferecer como páginas
            List<SpellData> lockedSpells = new();
            if (db != null)
            {
                try
                {
                    lockedSpells = db.Query<SpellData>(
                        "SELECT * FROM Magias WHERE Desbloqueado = 0").ToList();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[RewardGenerator] Erro ao buscar magias: {e.Message}");
                }
            }

            for (int i = 0; i < count; i++)
            {
                // Sorteia o tipo. Se não há magias bloqueadas, não oferece página.
                int maxType = lockedSpells.Count > 0 ? 3 : 2;
                RewardType type = (RewardType)Random.Range(0, maxType);

                switch (type)
                {
                    case RewardType.Armor:
                        items.Add(MakeArmor());
                        break;

                    case RewardType.Staff:
                        items.Add(MakeStaff());
                        break;

                    case RewardType.Page:
                        // Pega uma magia bloqueada aleatória (sem repetir as já escolhidas nesta tela)
                        var available = lockedSpells
                            .Where(s => !items.Any(it => it.Type == RewardType.Page && it.SpellId == s.Id))
                            .ToList();

                        if (available.Count > 0)
                        {
                            var spell = available[Random.Range(0, available.Count)];
                            items.Add(MakePage(spell));
                        }
                        else
                        {
                            // Fallback: se acabaram as magias, oferece cajado
                            items.Add(MakeStaff());
                        }
                        break;
                }
            }

            return items;
        }

        private static RewardItem MakeArmor()
        {
            return new RewardItem
            {
                Type = RewardType.Armor,
                Title = "Armadura Rúnica",
                Description = "Reduz o dano recebido em 12%."
            };
        }

        private static RewardItem MakeStaff()
        {
            string element = Elements[Random.Range(0, Elements.Length)];
            return new RewardItem
            {
                Type = RewardType.Staff,
                Element = element,
                Title = $"Cajado de {element}",
                Description = $"Aumenta o dano de magias de {element} em 25%."
            };
        }

        private static RewardItem MakePage(SpellData spell)
        {
            return new RewardItem
            {
                Type = RewardType.Page,
                SpellId = spell.Id,
                SpellName = spell.Nome,
                Element = spell.Elemento,
                Title = $"Página: {spell.Nome}",
                Description = $"Desbloqueia a magia {spell.Nome} ({spell.Elemento}, {spell.Distancia}, {spell.DanoBase} dano)."
            };
        }

        /// <summary>Aplica o item escolhido aos stats do jogador / banco.</summary>
        public static void ApplyReward(RewardItem item)
        {
            switch (item.Type)
            {
                case RewardType.Armor:
                    PlayerStats.Instance?.AddArmor();
                    break;

                case RewardType.Staff:
                    PlayerStats.Instance?.AddStaff(item.Element);
                    break;

                case RewardType.Page:
                    UnlockSpell(item.SpellId);
                    break;
            }
        }

        private static void UnlockSpell(int spellId)
        {
            var db = DatabaseManager.Instance?.DB;
            if (db == null) return;

            try
            {
                db.Execute("UPDATE Magias SET Desbloqueado = 1 WHERE Id = ?", spellId);
                Debug.Log($"[RewardGenerator] Magia Id={spellId} desbloqueada.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RewardGenerator] Erro ao desbloquear magia: {e.Message}");
            }
        }
    }
}
