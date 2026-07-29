// Assets/_QueryQuest/Combat/SpiritCompanion.cs
// O espírito do grimório que acompanha o herói na arena.
// No início de cada combate, dá UMA dica aleatória sobre o inimigo.
//
// Visual: placeholder (um quadradinho acima do herói na ArenaSceneView).
// A dica aparece no log de combate e/ou num balão de fala (SpiritUI).

using UnityEngine;
using QueryQuest.Models;

namespace QueryQuest.Combat
{
    public class SpiritCompanion : MonoBehaviour
    {
        public static SpiritCompanion Instance { get; private set; }

        // Evento para a UI mostrar a fala do espírito (balão)
        public event System.Action<string> OnSpiritSpeak;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (CombatManager.Instance != null)
                CombatManager.Instance.OnStateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (CombatManager.Instance != null)
                CombatManager.Instance.OnStateChanged -= OnStateChanged;
        }

        private CombatState _lastState = CombatState.IDLE;

        private void OnStateChanged(CombatState state)
        {
            // Detecta o começo de um novo combate (IDLE → PLAYER_TURN pela 1ª vez)
            if (_lastState == CombatState.IDLE && state == CombatState.PLAYER_TURN)
            {
                GiveHint();
            }
            _lastState = state;
        }

        // As 3 dicas da partida atual (o jogador cicla entre elas clicando na lua)
        private readonly System.Collections.Generic.List<string> _hints = new System.Collections.Generic.List<string>();
        private string _hintsFor;
        private int _hintIndex;

        /// <summary>Faz a lua dizer uma mensagem qualquer (drops, avisos...).</summary>
        public void Say(string message)
        {
            if (!string.IsNullOrEmpty(message)) OnSpiritSpeak?.Invoke(message);
        }

        /// <summary>Primeira dica — no início do combate.</summary>
        public void GiveHint()
        {
            var enemy = CombatManager.Instance?.CurrentEnemy;
            if (enemy == null) return;

            BuildHints(enemy);
            if (_hints.Count == 0) return;

            _hintIndex = 0;
            OnSpiritSpeak?.Invoke(_hints[0]);

            // Só a primeira vai para o log (com o empurrão para a magia Analise)
            CombatManager.Instance?.LogExternal($"[ESPIRITO] {_hints[0]} {AnaliseNudge}");
        }

        /// <summary>Próxima dica — chamado ao clicar na lua. Cicla em ordem.</summary>
        public void NextHint()
        {
            var enemy = CombatManager.Instance?.CurrentEnemy;
            if (enemy == null) return;

            BuildHints(enemy);
            if (_hints.Count == 0) return;

            _hintIndex = (_hintIndex + 1) % _hints.Count;
            OnSpiritSpeak?.Invoke(_hints[_hintIndex]);
        }

        /// <summary>Monta as 3 dicas do inimigo atual (uma vez por combate).</summary>
        private void BuildHints(EnemyData enemy)
        {
            if (_hintsFor == enemy.Nome && _hints.Count > 0) return;

            _hintsFor = enemy.Nome;
            _hintIndex = 0;
            _hints.Clear();

            _hints.Add(ElementHint(enemy));
            _hints.Add(DistanceHint(enemy));
            _hints.Add(AttackHint(enemy));
        }

        private const string AnaliseNudge =
            "Confirme com a magia 'Analise', mas cuidado: se não souber o que buscar, gasta mais mana do que pensa.";

        // Dicas LEVES: nunca entregam o dado cru, só apontam a direção.
        // Curtas o bastante para caber no balão de fala.

        private string ElementHint(EnemyData enemy)
        {
            string clue = (enemy.FraquezaElemento ?? "").ToLower() switch
            {
                "fogo"  => "o calor das chamas faz os cristais dele racharem",
                "agua"  => "algo em água corrente incomoda esses cristais",
                "vento" => "lâminas de vento parecem cortar bem essa pedra",
                "terra" => "o peso da própria terra derruba esse tipo de golem",
                "raio"  => "uma faísca certeira desperta algo ruim nesses cristais",
                _       => "há um elemento que ele não suporta"
            };
            return $"Sinto que {clue}.";
        }

        private string DistanceHint(EnemyData enemy)
        {
            string clue = enemy.FraquezaDistancia?.ToUpper() switch
            {
                "CURTO" => "ele se atrapalha quando encaram ele de perto",
                "MEDIO" => "a guarda dele falha a uma distância média",
                "LONGO" => "ele não sabe se defender do que vem de longe",
                _       => "a posição em que você luta com ele importa"
            };
            return $"Repare: {clue}.";
        }

        private string AttackHint(EnemyData enemy)
        {
            string clue = enemy.AtaqueDistancia?.ToUpper() switch
            {
                "CURTO" => "o golpe dele só alcança quem está colado",
                "MEDIO" => "o golpe dele alcança a média distância",
                "LONGO" => "o golpe dele te acerta mesmo de longe",
                _       => "o alcance do golpe dele é imprevisível"
            };
            return $"Cuidado: {clue}.";
        }
    }
}
