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
        /// Gera 3 itens aleatórios, todos DIFERENTES entre si.
        ///
        /// Antes o tipo era sorteado a cada vaga sem olhar o que já havia saído,
        /// e como só existe uma armadura, duas vagas de armadura produziam dois
        /// cartões idênticos — o mesmo valia para dois cajados do mesmo elemento.
        /// Agora cada vaga sorteia apenas entre o que ainda tem algo distinto a
        /// oferecer: a armadura sai da lista depois de escolhida, o cajado sorteia
        /// entre os elementos ainda livres e a página entre as magias restantes.
        ///
        /// A oferta mínima é de 6 itens distintos (1 armadura + 5 cajados), então
        /// as três vagas sempre são preenchidas.
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
                // O que ainda pode render um item INÉDITO nesta tela
                var elementosLivres = Elements
                    .Where(e => !items.Any(it => it.Type == RewardType.Staff && it.Element == e))
                    .ToList();

                var magiasLivres = lockedSpells
                    .Where(s => !items.Any(it => it.Type == RewardType.Page && it.SpellId == s.Id))
                    .ToList();

                var tipos = new List<RewardType>();
                if (!items.Any(it => it.Type == RewardType.Armor)) tipos.Add(RewardType.Armor);
                if (elementosLivres.Count > 0) tipos.Add(RewardType.Staff);
                if (magiasLivres.Count > 0) tipos.Add(RewardType.Page);

                // Só aconteceria com a lista de elementos vazia; melhor devolver
                // menos cartões do que repetir um.
                if (tipos.Count == 0) break;

                switch (tipos[Random.Range(0, tipos.Count)])
                {
                    case RewardType.Armor:
                        items.Add(MakeArmor());
                        break;

                    case RewardType.Staff:
                        items.Add(MakeStaff(elementosLivres[Random.Range(0, elementosLivres.Count)]));
                        break;

                    case RewardType.Page:
                        items.Add(MakePage(magiasLivres[Random.Range(0, magiasLivres.Count)]));
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

        /// <summary>O elemento vem de fora: quem sorteia já descartou os repetidos.</summary>
        private static RewardItem MakeStaff(string element)
        {
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
