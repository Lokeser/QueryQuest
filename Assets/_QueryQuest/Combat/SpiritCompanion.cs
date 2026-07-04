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

        /// <summary>Dá uma dica aleatória sobre o inimigo atual.</summary>
        public void GiveHint()
        {
            var enemy = CombatManager.Instance?.CurrentEnemy;
            if (enemy == null) return;

            string hint = GenerateHint(enemy);
            string message = $"[ESPIRITO] {hint}";

            CombatManager.Instance?.LogExternal(message);
            OnSpiritSpeak?.Invoke(hint);
        }

        private string GenerateHint(EnemyData enemy)
        {
            // A dica sempre aponta para uma característica real do inimigo,
            // mas incentiva o uso da magia Analise (com aviso sobre a mana).
            string warning = "Tente ver ele com a magia 'Analise', mas cuidado: se você não souber o que buscar nele, pode consumir mais mana do que pensa.";

            int roll = Random.Range(0, 4);

            switch (roll)
            {
                case 0:
                    return $"Esse ser não aguenta bem o elemento {enemy.FraquezaElemento}... {warning}";

                case 1:
                    string distHint = enemy.FraquezaDistancia?.ToUpper() switch
                    {
                        "CURTO" => "parece sofrer mais de perto",
                        "MEDIO" => "parece vulnerável a média distância",
                        "LONGO" => "parece frágil contra ataques de longe",
                        _ => "tem uma fraqueza de posição estranha"
                    };
                    return $"Esse {distHint}. {warning}";

                case 2:
                    string atkHint = enemy.AtaqueDistancia?.ToUpper() switch
                    {
                        "CURTO" => "só alcança quem está colado nele",
                        "MEDIO" => "golpeia a média distância",
                        "LONGO" => "ataca de muito longe",
                        _ => "tem um ataque imprevisível"
                    };
                    return $"O golpe desse inimigo {atkHint}. {warning}";

                case 3:
                    return $"Sinto algo perigoso em {enemy.Nome}. {warning}";

                default:
                    return $"Que a lógica te guie, herói. {warning}";
            }
        }
    }
}
