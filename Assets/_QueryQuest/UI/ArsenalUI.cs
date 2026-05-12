// Assets/_QueryQuest/UI/ArsenalUI.cs
// Exibe os dados brutos da tabela Feiticos como no resultado de SELECT * FROM Feiticos.
// Serve como referência educacional — o jogador vê exatamente o que o banco contém.
//
// Hierarquia esperada em PanelArsenal:
//
// PanelArsenal  (este script)
// ├── TableHeader           (GameObject com 6 TextMeshProUGUI filhos: Id, Nome, Elemento, Nivel, Distancia, DanoBase)
// └── TableScrollView
//     └── Viewport
//         └── TableContent  (RectTransform + VerticalLayoutGroup)
//             └── RowPrefab (instanciado por código — mesmo layout do Header)

using System.Collections.Generic;
using UnityEngine;
using TMPro;
using QueryQuest.Database;
using QueryQuest.Models;

namespace QueryQuest.UI
{
    public class ArsenalUI : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private RectTransform tableContent;
        [SerializeField] private GameObject rowPrefab;

        [Header("Cor da coluna Id")]
        [SerializeField] private Color idColor = new Color(0.33f, 0.29f, 0.72f); // roxo

        private readonly List<GameObject> _rows = new();

        public void Refresh()
        {
            ClearRows();

            if (DatabaseManager.Instance?.DB == null) return;

            var spells = DatabaseManager.Instance.DB.Table<SpellData>().ToList();
            spells.Sort((a, b) => a.Id.CompareTo(b.Id));

            foreach (var spell in spells)
                SpawnRow(spell);
        }

        private void SpawnRow(SpellData spell)
        {
            if (rowPrefab == null || tableContent == null) return;

            var row = Instantiate(rowPrefab, tableContent);
            _rows.Add(row);

            // Espera que o prefab tenha filhos nomeados Col0..Col5
            SetCol(row, "Col0", spell.Id.ToString(),        idColor);
            SetCol(row, "Col1", spell.Nome,                 Color.white);
            SetCol(row, "Col2", spell.Elemento,             Color.white);
            SetCol(row, "Col3", spell.Nivel.ToString(),     Color.white);
            SetCol(row, "Col4", spell.Distancia,            Color.white);
            SetCol(row, "Col5", spell.DanoBase.ToString(),  Color.white);
        }

        private void SetCol(GameObject row, string colName, string value, Color color)
        {
            var child = row.transform.Find(colName);
            if (child == null) return;
            var tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp == null) return;
            tmp.text  = value;
            tmp.color = color;
        }

        private void ClearRows()
        {
            foreach (var r in _rows) Destroy(r);
            _rows.Clear();
        }
    }
}
