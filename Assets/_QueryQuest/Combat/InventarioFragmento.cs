// Assets/_QueryQuest/Combat/InventarioFragmento.cs
// Guarda os fragmentos que os golens deixaram cair durante a run.
// Acumula entre andares e só zera quando a run termina (vitória ou derrota).
// Criado sozinho ao carregar a cena — nada para configurar no Inspector.

using System;
using System.Collections.Generic;
using UnityEngine;
using QueryQuest.Database;
using QueryQuest.Models;

namespace QueryQuest.Combat
{
    public class InventarioFragmento : MonoBehaviour
    {
        public static InventarioFragmento Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("~InventarioFragmento");
            go.AddComponent<InventarioFragmento>();
        }

        private readonly Dictionary<int, int> _quantidades = new Dictionary<int, int>();

        public event Action OnInventarioChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public int TotalFragmentos
        {
            get
            {
                int total = 0;
                foreach (var qtd in _quantidades.Values) total += qtd;
                return total;
            }
        }

        public void Adicionar(int fragmentoID, int qtd = 1)
        {
            if (_quantidades.ContainsKey(fragmentoID)) _quantidades[fragmentoID] += qtd;
            else _quantidades[fragmentoID] = qtd;

            OnInventarioChanged?.Invoke();
        }

        /// <summary>Consome um fragmento (usado ao absorvê-lo com o JOIN).</summary>
        public bool Remover(int fragmentoID)
        {
            if (!_quantidades.TryGetValue(fragmentoID, out int qtd) || qtd <= 0) return false;

            if (qtd <= 1) _quantidades.Remove(fragmentoID);
            else _quantidades[fragmentoID] = qtd - 1;

            OnInventarioChanged?.Invoke();
            return true;
        }

        public int Quantidade(int fragmentoID)
            => _quantidades.TryGetValue(fragmentoID, out int qtd) ? qtd : 0;

        /// <summary>Os fragmentos que o jogador tem, já lidos do banco.</summary>
        public List<FragmentoData> ObterTodos()
        {
            var resultado = new List<FragmentoData>();
            var db = DatabaseManager.Instance?.DB;
            if (db == null) return resultado;

            foreach (var id in _quantidades.Keys)
            {
                var frag = db.Find<FragmentoData>(id);
                if (frag != null) resultado.Add(frag);
            }

            resultado.Sort((a, b) =>
            {
                int r = b.Raridade.CompareTo(a.Raridade);     // mais raros primeiro
                return r != 0 ? r : string.Compare(a.Nome, b.Nome, StringComparison.Ordinal);
            });
            return resultado;
        }

        public void Resetar()
        {
            _quantidades.Clear();
            OnInventarioChanged?.Invoke();
        }
    }
}
