// Assets/_QueryQuest/UI/MagiaListUI.cs
// Hierarquia esperada em PanelMagias:
//
// PanelMagias  (este script)
// └── SpellScrollView
//     └── Viewport
//         └── SpellContent       (RectTransform + VerticalLayoutGroup)
//             └── SpellRowPrefab (instanciado por código)
//
// SpellRowPrefab deve ter:
//   ├── NameText        (TMP) — nome do feitiço
//   ├── ElementBadge    (Image + Text TMP) — elemento colorido
//   ├── LevelText       (TMP) — "Nv.X"
//   ├── DistText        (TMP) — CURTO/MEDIO/LONGO
//   ├── DamageText      (TMP) — dano base
//   └── StatusBadge     (Image + Text TMP) — PRONTO / BLOQ.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Database;
using QueryQuest.Models;

namespace QueryQuest.UI
{
    public class MagiaListUI : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private RectTransform spellContent;
        [SerializeField] private GameObject spellRowPrefab;

        [Header("Cores de Elemento")]
        [SerializeField] private Color colorFogo = new Color(0.94f, 0.58f, 0.58f);
        [SerializeField] private Color colorAgua = new Color(0.52f, 0.71f, 0.92f);
        [SerializeField] private Color colorVento = new Color(0.36f, 0.79f, 0.65f);
        [SerializeField] private Color colorTerra = new Color(0.98f, 0.78f, 0.46f);
        [SerializeField] private Color colorRaio = new Color(0.69f, 0.66f, 0.93f);

        [Header("Cores de Status")]
        [SerializeField] private Color colorUnlocked = new Color(0.36f, 0.79f, 0.65f);
        [SerializeField] private Color colorLocked = new Color(0.37f, 0.37f, 0.35f);

        private readonly List<GameObject> _rows = new();

        // ─────────────────────────────────────────────────────────────────────
        // API PÚBLICA
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Recarrega a lista do banco e reconstrói as linhas.</summary>
        public void Refresh()
        {
            ClearRows();

            if (DatabaseManager.Instance?.DB == null) return;

            var spells = DatabaseManager.Instance.DB.Table<SpellData>().ToList();

            // Ordenação: desbloqueados primeiro, depois por elemento e nível
            spells.Sort((a, b) =>
            {
                if (a.Desbloqueado != b.Desbloqueado) return b.Desbloqueado - a.Desbloqueado;
                int elemComp = string.Compare(a.Elemento, b.Elemento, System.StringComparison.Ordinal);
                return elemComp != 0 ? elemComp : a.Nivel.CompareTo(b.Nivel);
            });

            foreach (var spell in spells)
                SpawnRow(spell);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CRIAÇÃO DE LINHAS
        // ─────────────────────────────────────────────────────────────────────

        private void SpawnRow(SpellData spell)
        {
            if (spellRowPrefab == null || spellContent == null) return;

            var row = Instantiate(spellRowPrefab, spellContent);
            _rows.Add(row);

            bool unlocked = spell.Desbloqueado == 1;

            // Aplica opacidade em feitiços bloqueados
            var cg = row.GetComponent<CanvasGroup>();
            if (cg == null) cg = row.AddComponent<CanvasGroup>();
            cg.alpha = unlocked ? 1f : 0.45f;

            // Preenche os campos pelo nome do GameObject filho
            SetChildText(row, "NameText", spell.Nome);
            SetChildText(row, "ElementBadge", spell.Elemento);
            SetChildText(row, "LevelText", "Nv." + spell.Nivel);
            SetChildText(row, "DistText", spell.Distancia);
            SetChildText(row, "DamageText", spell.DanoBase.ToString());
            SetChildText(row, "StatusBadge", unlocked ? "PRONTO" : "BLOQ.");

            // Cor do elemento no badge
            SetChildColor(row, "ElementBadge", GetElementColor(spell.Elemento));

            // Cor do status
            SetChildColor(row, "StatusBadge", unlocked ? colorUnlocked : colorLocked);
        }

        private void ClearRows()
        {
            foreach (var r in _rows) Destroy(r);
            _rows.Clear();
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private void SetChildText(GameObject parent, string childName, string value)
        {
            var child = parent.transform.Find(childName);
            if (child == null) return;
            var tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = value;
        }

        private void SetChildColor(GameObject parent, string childName, Color color)
        {
            var child = parent.transform.Find(childName);
            if (child == null) return;
            var tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.color = color;
        }

        private Color GetElementColor(string elemento) => elemento?.ToLower() switch
        {
            "fogo" => colorFogo,
            "agua" => colorAgua,
            "vento" => colorVento,
            "terra" => colorTerra,
            "raio" => colorRaio,
            _ => Color.white
        };
    }
}