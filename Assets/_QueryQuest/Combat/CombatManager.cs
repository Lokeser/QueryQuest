// Assets/_QueryQuest/Combat/CombatManager.cs
using System;
using UnityEngine;
using QueryQuest.Database;
using QueryQuest.Models;

namespace QueryQuest.Combat
{
    /// <summary>
    /// Máquina de estados central do combate.
    /// Comunica com TurnController para sequenciar turnos,
    /// e expõe eventos para a UI reagir sem acoplamento direto.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        // ─── Singleton -----------------------------───────────────────────────
        public static CombatManager Instance { get; private set; }

        // ─── Estado atual -----------------------------────────────────────────
        public CombatState CurrentState { get; private set; } = CombatState.IDLE;

        // ─── Dados do combate em andamento -----------------------------───────
        public EnemyData   CurrentEnemy    { get; private set; }
        public int         EnemyCurrentHP  { get; private set; }
        public int         PlayerCurrentHP { get; private set; }
        public int         PlayerMaxHP     { get; private set; } = 100;
        public string      CurrentDistance { get; private set; } = "MEDIO";

        // ─── Referências -----------------------------─────────────────────────
        private SQLInterpreter _interpreter;
        private TurnController _turnController;
        private SlotSystem _slots;

        // ─── Eventos (a UI se inscreve aqui) -----------------------------─────
        public event Action<CombatState>  OnStateChanged;
        public event Action<string>       OnCombatLog;          // mensagens para o log de combate
        public event Action<DamageResult, bool> OnDamageApplied; // resultado, isPlayerAttacking
        public event Action<bool>         OnCombatEnded;        // true = jogador venceu

        // ----------------------------------------------------------───────────
        // UNITY
        // ----------------------------------------------------------───────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _turnController = GetComponent<TurnController>();
            if (_turnController == null)
                _turnController = gameObject.AddComponent<TurnController>();

            _interpreter = new SQLInterpreter(DatabaseManager.Instance.DB);

            _slots = SlotSystem.Instance;
            if (_slots == null)
                Debug.LogError("[CombatManager] SlotSystem nao encontrado! Adicione ao GameManager.");
        }

        // ----------------------------------------------------------───────────
        // API PÚBLICA
        // ----------------------------------------------------------───────────

        /// <summary>Inicia um combate contra o inimigo especificado.</summary>
        public void StartCombat(EnemyData enemy)
        {
            CurrentEnemy   = enemy;
            EnemyCurrentHP = enemy.HP;
            PlayerCurrentHP = PlayerMaxHP;
            CurrentDistance = "MEDIO";

            Log($"[COMBATE] Combate iniciado contra {enemy.Nome}!");
            Log($"[HP] Seu HP: {PlayerCurrentHP}/{PlayerMaxHP}  |  [DERROTA] {enemy.Nome}: {EnemyCurrentHP}/{enemy.HP}");
            Log("-----------------------------");
            Log("Seu turno! Mova-se e abra o grimório para atacar.");

            _slots?.ResetPositions();
            TransitionTo(CombatState.PLAYER_TURN);
        }

        /// <summary>Jogador abre o grimório durante seu turno.</summary>
        public void OpenGrimoire()
        {
            if (CurrentState != CombatState.PLAYER_TURN)
            {
                Log("O grimório só pode ser aberto no seu turno.");
                return;
            }
            TransitionTo(CombatState.GRIMOIRE_OPEN);
        }

        /// <summary>Fecha o grimório sem executar nenhum feitiço.</summary>
        public void CloseGrimoire()
        {
            if (CurrentState != CombatState.GRIMOIRE_OPEN) return;
            TransitionTo(CombatState.PLAYER_TURN);
        }

        /// <summary>
        /// Jogador submete uma query SQL no grimório.
        /// Se a query retornar exatamente 1 feitiço desbloqueado, lança-o.
        /// </summary>
        public QueryResult SubmitQuery(string rawQuery)
        {
            if (CurrentState != CombatState.GRIMOIRE_OPEN)
                return QueryResult.Error("O grimório não está aberto.");

            TransitionTo(CombatState.QUERY_EXECUTING);

            var result = _interpreter.Execute(rawQuery);
            Log(result.FormattedLog);

            if (!result.Success)
            {
                TransitionTo(CombatState.GRIMOIRE_OPEN);
                return result;
            }

            if (result.SelectedSpell == null)
            {
                // Query válida mas retornou 0 ou múltiplos feitiços — não lança
                string hint = result.Rows.Count == 0
                    ? "Nenhum feitiço encontrado. Refine sua query."
                    : $"{result.Rows.Count} feitiços encontrados. Use filtros para selecionar exatamente 1.";

                Log($"[AVISO] {hint}");
                TransitionTo(CombatState.GRIMOIRE_OPEN);
                return result;
            }

            if (result.SelectedSpell.Desbloqueado == 0)
            {
                Log($"[AVISO] {result.SelectedSpell.Nome} ainda não foi desbloqueado.");
                TransitionTo(CombatState.GRIMOIRE_OPEN);
                return result;
            }

            // Feitiço válido → executa
            CastSpell(result.SelectedSpell);
            return result;
        }

        /// <summary>Muda a distância do combate (ação de turno alternativa).</summary>
        public void ChangeDistance(string newDistance)
        {
            if (CurrentState != CombatState.PLAYER_TURN) return;

            CurrentDistance = newDistance.ToUpper();
            Log($"[DISTANCIA] Distância alterada para {CurrentDistance}.");
            EndPlayerTurn();
        }

        /// <summary>Ação de fuga (sempre no turno do jogador).</summary>
        public void TryFlee()
        {
            if (CurrentState != CombatState.PLAYER_TURN) return;

            bool success = UnityEngine.Random.value > 0.4f;
            Log(success
                ? "[FUGA] Fuga bem-sucedida! SELECT * FROM Fuga_Log WHERE Sucesso = TRUE"
                : "❌ Fuga falhou! O inimigo bloqueou a saída.");

            if (success) EndCombat(playerWon: false, fled: true);
            else EndPlayerTurn();
        }

        // ----------------------------------------------------------───────────
        // FLUXO INTERNO
        // ----------------------------------------------------------───────────

        private void CastSpell(SpellData spell)
        {
            TransitionTo(CombatState.SPELL_CAST);
            Log($"[Magia] Lançando {spell.Nome} ({spell.Elemento}) a distância {CurrentDistance}...");

            var dmgResult = DamageCalculator.Calculate(spell, CurrentEnemy, CurrentDistance);

            Log($"[DANO] {dmgResult.Breakdown}");
            Log($"[EFEITO] {dmgResult.Effectiveness}");

            ApplyDamageToEnemy(dmgResult);
        }

        private void ApplyDamageToEnemy(DamageResult dmgResult)
        {
            TransitionTo(CombatState.APPLYING_DAMAGE);

            EnemyCurrentHP = Mathf.Max(0, EnemyCurrentHP - dmgResult.FinalDamage);
            OnDamageApplied?.Invoke(dmgResult, true);

            Log($"[DANO] {CurrentEnemy.Nome} HP: {EnemyCurrentHP}/{CurrentEnemy.HP} (-{dmgResult.FinalDamage})");
            CheckResult(wasPlayerAttacking: true);
        }

        private void ApplyDamageToPlayer(int damage)
        {
            TransitionTo(CombatState.APPLYING_DAMAGE);

            var fakeResult = new DamageResult { FinalDamage = damage };
            PlayerCurrentHP = Mathf.Max(0, PlayerCurrentHP - damage);
            OnDamageApplied?.Invoke(fakeResult, false);

            Log($"[DANO] Você sofreu {damage} de dano. HP: {PlayerCurrentHP}/{PlayerMaxHP}");
            CheckResult(wasPlayerAttacking: false);
        }

        private void CheckResult(bool wasPlayerAttacking)
        {
            TransitionTo(CombatState.CHECKING_RESULT);

            if (EnemyCurrentHP <= 0)
            {
                Log($"[VITORIA] {CurrentEnemy.Nome} foi derrotado!");
                EndCombat(playerWon: true);
                return;
            }

            if (PlayerCurrentHP <= 0)
            {
                Log("[DERROTA] Você foi derrotado...");
                EndCombat(playerWon: false);
                return;
            }

            // Combate continua
            if (wasPlayerAttacking)
                StartEnemyTurn();   // jogador atacou → agora é o turno do inimigo
            else
                StartPlayerTurn();  // inimigo atacou → volta ao turno do jogador
        }

        private void StartPlayerTurn()
        {
            Log("-----------------------------");
            Log("[COMBATE] Seu turno! Abra o grimório e escolha um feitiço.");
            Log($"[HP] Seu HP: {PlayerCurrentHP}/{PlayerMaxHP}  |  [DERROTA] {CurrentEnemy.Nome}: {EnemyCurrentHP}/{CurrentEnemy.HP}");
            TransitionTo(CombatState.PLAYER_TURN);
        }

        private void StartEnemyTurn()
        {
            TransitionTo(CombatState.ENEMY_TURN);

            // Inimigo se move 1 slot em direcao ao jogador
            _slots?.MoveEnemyTowardsPlayer();
            if (_slots != null)
                Log($"[INIMIGO] {CurrentEnemy.Nome} avanca! {_slots.GetStatusString()}");

            _turnController.ExecuteEnemyTurn(CurrentEnemy, (damage) =>
            {
                Log($"[INIMIGO] {CurrentEnemy.Nome} ataca!");
                ApplyDamageToPlayer(damage);
            });
        }

        private void EndPlayerTurn()
        {
            StartEnemyTurn();
        }

        private void EndCombat(bool playerWon, bool fled = false)
        {
            TransitionTo(CombatState.IDLE);
            string msg = fled ? "Você fugiu do combate."
                       : playerWon ? "Vitória!" : "Derrota...";
            Log($"── {msg} ──");
            OnCombatEnded?.Invoke(playerWon);
        }

        // ----------------------------------------------------------───────────
        // HELPERS
        // ----------------------------------------------------------───────────

        private void TransitionTo(CombatState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
            Debug.Log($"[CombatManager] → {newState}");
        }

        private void Log(string msg)
        {
            OnCombatLog?.Invoke(msg);
            Debug.Log($"[Combat] {msg}");
        }
    }
}
