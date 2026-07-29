// Assets/_QueryQuest/Combat/FragmentDropSystem.cs
// Sorteia os fragmentos que o golem derrotado deixa cair e faz a lua
// avisar o jogador para absorvê-los com o grimório (JOIN).
//
// Chance de sair, por raridade: 1=80%, 2=60%, 3=40%, 4=20%, 5=5%.
// Sempre cai pelo menos um fragmento — o golem nunca morre "de mãos vazias".

using System.Collections.Generic;
using UnityEngine;
using QueryQuest.Database;
using QueryQuest.Models;

namespace QueryQuest.Combat
{
    public class FragmentDropSystem : MonoBehaviour
    {
        public static FragmentDropSystem Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("~FragmentDrops");
            go.AddComponent<FragmentDropSystem>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// A UI escuta isto para abrir a tela de absorção com o que caiu.
        /// É ESTÁTICO de propósito: a tela nasce desativada (e portanto assina no
        /// Awake), então não dá para depender de já existir uma instância aqui.
        /// </summary>
        public static event System.Action<List<FragmentoData>> OnAbsorbPhaseStarted;

        private System.Action _onPhaseDone;

        // O inventário NÃO zera ao vencer o andar 5: o Modo Infinito continua a
        // mesma build. Quem limpa é o FloorManager.RestartRun (run nova de fato).

        /// <summary>
        /// A cena de pós-vitória: o golem cai, os fragmentos dele caem junto, a lua
        /// manda absorver e a tela de absorção abre. Só quando o jogador termina
        /// (onDone) é que a recompensa/próximo andar seguem.
        /// </summary>
        public void BeginAbsorbPhase(EnemyData enemy, System.Action onDone)
        {
            _onPhaseDone = onDone;

            if (enemy == null) { FinishPhase(); return; }

            var caidos = SortearDrops(enemy);
            foreach (var frag in caidos)
            {
                InventarioFragmento.Instance?.Adicionar(frag.FragmentoID);
                CombatManager.Instance?.LogExternal(
                    $"[DROP] {enemy.Nome} deixou cair: {frag.Nome} (raridade {frag.Raridade}).");
            }

            AnunciarPelaLua(enemy, caidos);

            // Sem ninguém para mostrar a tela, o jogo não pode travar aqui
            if (OnAbsorbPhaseStarted == null || caidos.Count == 0) { FinishPhase(); return; }

            StartCoroutine(AbrirTelaDepoisDaFala(caidos));
        }

        private System.Collections.IEnumerator AbrirTelaDepoisDaFala(List<FragmentoData> caidos)
        {
            // Deixa o golem colapsar e a fala da lua aparecer antes da tela
            yield return new WaitForSeconds(1.4f);
            OnAbsorbPhaseStarted?.Invoke(caidos);
        }

        /// <summary>Chamado pela tela de absorção quando o jogador termina.</summary>
        public void FinishPhase()
        {
            var callback = _onPhaseDone;
            _onPhaseDone = null;
            callback?.Invoke();
        }

        // ─────────────────────────────────────────────────────────────────────
        // SORTEIO
        // ─────────────────────────────────────────────────────────────────────

        private List<FragmentoData> SortearDrops(EnemyData enemy)
        {
            var resultado = new List<FragmentoData>();

            var db = DatabaseManager.Instance?.DB;
            if (db == null) return resultado;

            var pool = db.Query<FragmentoData>(
                "SELECT * FROM Fragmentos WHERE InimigoID = ?", enemy.Id);
            if (pool == null || pool.Count == 0) return resultado;

            int qtd = Random.Range(1, 3);   // 1 ou 2 fragmentos
            for (int i = 0; i < qtd; i++)
            {
                var candidatos = new List<FragmentoData>();
                foreach (var f in pool)
                    if (Random.value < ChancePorRaridade(f.Raridade)) candidatos.Add(f);

                if (candidatos.Count > 0)
                    resultado.Add(candidatos[Random.Range(0, candidatos.Count)]);
            }

            // Garante ao menos um drop: pega o mais comum do pool
            if (resultado.Count == 0)
            {
                var maisComum = pool[0];
                foreach (var f in pool)
                    if (f.Raridade < maisComum.Raridade) maisComum = f;
                resultado.Add(maisComum);
            }

            return resultado;
        }

        private static float ChancePorRaridade(int raridade)
        {
            switch (raridade)
            {
                case 1:  return 0.80f;
                case 2:  return 0.60f;
                case 3:  return 0.40f;
                case 4:  return 0.20f;
                default: return 0.05f;   // 5 = lendário
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // FALA DA LUA
        // ─────────────────────────────────────────────────────────────────────

        private void AnunciarPelaLua(EnemyData enemy, List<FragmentoData> caidos)
        {
            var floor = FloorManager.Instance;
            bool isBoss = floor != null && floor.CurrentFloor >= floor.TotalFloors;

            string msg = isBoss
                ? "Bom trabalho! O primordial caiu e deixou a essência dele. Absorva com o grimório: use o JOIN para tomar o poder dele!"
                : $"Bom trabalho! Ele deixou uma essência de {enemy.Elemento} cair. Absorva com o grimório usando o JOIN para te fortalecer!";

            SpiritCompanion.Instance?.Say(msg);
        }
    }
}
