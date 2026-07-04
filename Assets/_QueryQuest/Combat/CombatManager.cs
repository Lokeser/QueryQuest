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
        public event Action              OnHealthChanged;      // dispara quando HP de qualquer lado muda

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
            ManaSystem.Instance?.ResetMana();
            OnHealthChanged?.Invoke();
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

            // ── ANÁLISE: query na tabela Inimigos revela info do inimigo atual ──
            // e consome o turno (ação de análise no lugar do ataque).
            if (IsInimigosQuery(rawQuery))
            {
                HandleAnalise(rawQuery);
                return result;
            }

            if (result.SelectedSpell == null)
            {
                // Query válida mas retornou 0 feitiços — não lança
                Log("[AVISO] Nenhum feitiço encontrado. Refine sua query.");
                TransitionTo(CombatState.GRIMOIRE_OPEN);
                return result;
            }

            if (result.SelectedSpell.Desbloqueado == 0)
            {
                Log($"[AVISO] {result.SelectedSpell.Nome} ainda não foi desbloqueado.");
                TransitionTo(CombatState.GRIMOIRE_OPEN);
                return result;
            }

            // Feitiço utilitário (Analise) não é lançável
            if (result.SelectedSpell.Elemento == "Neutro" || result.SelectedSpell.Nome == "Analise")
            {
                Log("[AVISO] Para analisar, consulte a tabela Inimigos! Ex: SELECT Elemento FROM Inimigos");
                TransitionTo(CombatState.GRIMOIRE_OPEN);
                return result;
            }

            // ── CUSTO DE MANA por especificidade da query ──
            int manaCost = ManaSystem.Instance?.CalculateCost(rawQuery) ?? 0;

            // Query genérica (retornou vários) = você lança a 1ª, mas avisa o desperdício
            if (result.ResultCount > 1)
                Log($"[AVISO] Query ampla! {result.ResultCount} magias retornadas — você lança a primeira ({result.SelectedSpell.Nome}), mas gasta muita mana.");

            if (ManaSystem.Instance != null)
            {
                Log($"[MANA] {ManaSystem.Instance.ExplainCost(rawQuery)}");

                if (!ManaSystem.Instance.HasMana(manaCost))
                {
                    Log($"[MANA] Mana insuficiente! Você tem {ManaSystem.Instance.CurrentMana}, precisa de {manaCost}. Refine a query para baratear o custo!");
                    TransitionTo(CombatState.GRIMOIRE_OPEN);
                    return result;
                }

                ManaSystem.Instance.SpendMana(manaCost);
                Log($"[MANA] Mana restante: {ManaSystem.Instance.CurrentMana}/{ManaSystem.Instance.MaxMana}");
            }

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

        // ── ANÁLISE ──────────────────────────────────────────────────────────

        private bool IsInimigosQuery(string rawQuery)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(
                rawQuery, @"FROM\s+Inimigos", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Processa uma análise: revela as colunas do inimigo atual que o jogador
        /// pediu no SELECT, e consome o turno (equivale a atacar).
        /// </summary>
        private void HandleAnalise(string rawQuery)
        {
            // Extrai as colunas do SELECT
            var match = System.Text.RegularExpressions.Regex.Match(
                rawQuery, @"SELECT\s+(.+?)\s+FROM",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            string colsPart = match.Success ? match.Groups[1].Value.Trim() : "*";

            // Custo de mana: cada coluna analisada custa mana. SELECT * custa por TODAS.
            const int COST_PER_COLUMN = 8;
            const int ALL_COLUMNS = 7; // Nome, Nivel, Elemento, HP, Fraqueza, AtaqueDistancia, FraquezaDistancia

            int columnsAnalyzed;
            if (colsPart == "*")
                columnsAnalyzed = ALL_COLUMNS;
            else
                columnsAnalyzed = colsPart.Split(',').Length;

            int manaCost = columnsAnalyzed * COST_PER_COLUMN;

            if (ManaSystem.Instance != null)
            {
                if (!ManaSystem.Instance.HasMana(manaCost))
                {
                    Log($"[MANA] Análise custaria {manaCost} mana ({columnsAnalyzed} coluna(s)), mas você só tem {ManaSystem.Instance.CurrentMana}. Analise menos colunas!");
                    TransitionTo(CombatState.GRIMOIRE_OPEN);
                    return;
                }
                ManaSystem.Instance.SpendMana(manaCost);
                Log($"[ANALISE] Escaneando {columnsAnalyzed} atributo(s)... Custo: {manaCost} mana. Restante: {ManaSystem.Instance.CurrentMana}");
            }
            else
            {
                Log("[ANALISE] Escaneando o inimigo...");
            }

            if (colsPart == "*")
            {
                Log($"[ANALISE] Nome: {CurrentEnemy.Nome}");
                Log($"[ANALISE] Nivel: {CurrentEnemy.Nivel}");
                Log($"[ANALISE] Elemento: {CurrentEnemy.Elemento}");
                Log($"[ANALISE] Fraqueza Elemental: {ElementalSystem.GetWeaknessOf(CurrentEnemy.Elemento)}");
                Log($"[ANALISE] Ataque: Golpe {CapitalizeDistance(CurrentEnemy.AtaqueDistancia)}");
                Log($"[ANALISE] Vulnerável a ataques: {CapitalizeDistance(CurrentEnemy.FraquezaDistancia)}");
            }
            else
            {
                foreach (var raw in colsPart.Split(','))
                    RevealColumn(raw.Trim());
            }

            // Análise consome o turno → vai para o turno do inimigo
            EndPlayerTurn();
        }

        private void RevealColumn(string col)
        {
            switch (col.ToLower())
            {
                case "nome":
                    Log($"[ANALISE] Nome: {CurrentEnemy.Nome}");
                    break;
                case "nivel":
                    Log($"[ANALISE] Nivel: {CurrentEnemy.Nivel}");
                    break;
                case "elemento":
                    Log($"[ANALISE] Elemento: {CurrentEnemy.Elemento} (Fraqueza: {ElementalSystem.GetWeaknessOf(CurrentEnemy.Elemento)})");
                    break;
                case "hp":
                    Log($"[ANALISE] HP: {EnemyCurrentHP}/{CurrentEnemy.HP}");
                    break;
                case "fraquezaelemento":
                    Log($"[ANALISE] Fraqueza: {CurrentEnemy.FraquezaElemento}");
                    break;
                case "ataquedistancia":
                    Log($"[ANALISE] Ataque: Golpe {CapitalizeDistance(CurrentEnemy.AtaqueDistancia)} (alcança {GetReachFromDistance(CurrentEnemy.AtaqueDistancia)} slot(s))");
                    break;
                case "fraquezadistancia":
                    Log($"[ANALISE] Vulnerável a ataques {CapitalizeDistance(CurrentEnemy.FraquezaDistancia)} (+50% de dano nessa distância)");
                    break;
                case "descricao":
                    Log($"[ANALISE] {CurrentEnemy.Descricao}");
                    break;
                default:
                    Log($"[ANALISE] Coluna '{col}' desconhecida.");
                    break;
            }
        }

        private void CastSpell(SpellData spell)
        {
            TransitionTo(CombatState.SPELL_CAST);

            var targetSlots = _slots?.GetTargetSlots(spell.Distancia) ?? new System.Collections.Generic.List<int>();
            string slotsStr = targetSlots.Count > 0 ? string.Join(", ", targetSlots) : "nenhum";
            Log($"[MAGIA] Lançando {spell.Nome} ({spell.Elemento}, {spell.Distancia}) — atinge slot(s): {slotsStr}");

            // Flash vermelho de 2s nos slots atingidos
            if (_slots != null)
                StartCoroutine(FlashSpellRange(targetSlots));

            // Verifica se o inimigo está na área atingida
            bool hits = _slots?.SpellHitsEnemy(spell.Distancia) ?? true;
            if (!hits)
            {
                Log($"[ERRO] O inimigo (slot {_slots?.EnemySlot}) está fora da área da magia! Você errou.");
                EndPlayerTurn();
                return;
            }

            var dmgResult = DamageCalculator.Calculate(spell, CurrentEnemy, spell.Distancia);

            // Bônus de cajado elemental (item roguelike)
            float staffBonus = PlayerStats.Instance?.GetElementBonus(spell.Elemento) ?? 1f;
            if (staffBonus > 1f)
            {
                int boosted = Mathf.RoundToInt(dmgResult.FinalDamage * staffBonus);
                Log($"[DANO] Cajado de {spell.Elemento}: {dmgResult.FinalDamage} -> {boosted} (+{(staffBonus-1f):P0})");
                dmgResult.FinalDamage = boosted;
            }

            // Bônus por FRAQUEZA DE DISTÂNCIA do inimigo (+50% se acertar na distância fraca)
            if (!string.IsNullOrEmpty(CurrentEnemy.FraquezaDistancia) &&
                spell.Distancia.ToUpper() == CurrentEnemy.FraquezaDistancia.ToUpper())
            {
                int boosted = Mathf.RoundToInt(dmgResult.FinalDamage * 1.5f);
                Log($"[EFEITO] {CurrentEnemy.Nome} é vulnerável a ataques {spell.Distancia}! {dmgResult.FinalDamage} -> {boosted} (+50%)");
                dmgResult.FinalDamage = boosted;
            }

            Log($"[DANO] {dmgResult.Breakdown}");
            Log($"[EFEITO] {dmgResult.Effectiveness}");

            ApplyDamageToEnemy(dmgResult);
        }

        /// <summary>Acende os slots atingidos em vermelho por 2 segundos.</summary>
        private System.Collections.IEnumerator FlashSpellRange(System.Collections.Generic.List<int> slots)
        {
            _slots.ShowSpellRangeSlots(slots);
            yield return new WaitForSeconds(2f);
            _slots.HideSpellRange();
        }

        private void ApplyDamageToEnemy(DamageResult dmgResult)
        {
            TransitionTo(CombatState.APPLYING_DAMAGE);

            EnemyCurrentHP = Mathf.Max(0, EnemyCurrentHP - dmgResult.FinalDamage);
            OnDamageApplied?.Invoke(dmgResult, true);
            OnHealthChanged?.Invoke();

            Log($"[DANO] {CurrentEnemy.Nome} HP: {EnemyCurrentHP}/{CurrentEnemy.HP} (-{dmgResult.FinalDamage})");
            CheckResult(wasPlayerAttacking: true);
        }

        private void ApplyDamageToPlayer(int damage)
        {
            // Redução de dano por armadura (item roguelike)
            int original = damage;
            damage = PlayerStats.Instance?.ApplyDamageReduction(damage) ?? damage;
            if (damage < original)
                Log($"[DANO] Armadura absorveu {original - damage} de dano.");

            TransitionTo(CombatState.APPLYING_DAMAGE);

            var fakeResult = new DamageResult { FinalDamage = damage };
            PlayerCurrentHP = Mathf.Max(0, PlayerCurrentHP - damage);
            OnDamageApplied?.Invoke(fakeResult, false);
            OnHealthChanged?.Invoke();

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
            _slots?.ResetMovementAction();
            Log("-----------------------------");
            Log("[COMBATE] Seu turno! Mova-se e abra o grimório para atacar.");
            if (_slots != null)
                Log($"[HP] Seu HP: {PlayerCurrentHP}/{PlayerMaxHP}  |  [DERROTA] {CurrentEnemy.Nome}: {EnemyCurrentHP}/{CurrentEnemy.HP}  |  {_slots.GetStatusString()}");
            else
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

            // Verifica se o jogador está dentro do alcance do golpe do inimigo
            int distance = _slots?.GetDistance() ?? 0;
            int enemyReach = GetReachFromDistance(CurrentEnemy.AtaqueDistancia);
            bool enemyCanHit = distance <= enemyReach;

            _turnController.ExecuteEnemyTurn(CurrentEnemy, (damage) =>
            {
                if (enemyCanHit)
                {
                    Log($"[INIMIGO] {CurrentEnemy.Nome} usa Golpe {CapitalizeDistance(CurrentEnemy.AtaqueDistancia)}!");
                    ApplyDamageToPlayer(damage);
                }
                else
                {
                    Log($"[INIMIGO] {CurrentEnemy.Nome} tenta atacar, mas você está longe demais! (Golpe {CapitalizeDistance(CurrentEnemy.AtaqueDistancia)} alcança {enemyReach} slot(s), você está a {distance})");
                    // Ataque errou → volta ao turno do jogador sem dano
                    StartPlayerTurn();
                }
            });
        }

        /// <summary>Converte CURTO/MEDIO/LONGO em alcance de slots (1/2/3).</summary>
        private int GetReachFromDistance(string dist)
        {
            return (dist ?? "CURTO").ToUpper() switch
            {
                "CURTO" => 1,
                "MEDIO" => 2,
                "LONGO" => 3,
                _ => 1
            };
        }

        private string CapitalizeDistance(string dist)
        {
            return (dist ?? "CURTO").ToUpper() switch
            {
                "CURTO" => "Curto",
                "MEDIO" => "Médio",
                "LONGO" => "Longo",
                _ => "Curto"
            };
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

        /// <summary>Permite que outros sistemas (ex: SpiritCompanion) escrevam no log.</summary>
        public void LogExternal(string msg) => Log(msg);
    }
}
