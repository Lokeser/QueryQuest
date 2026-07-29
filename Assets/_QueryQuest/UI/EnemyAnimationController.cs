// Assets/_QueryQuest/UI/EnemyAnimationController.cs
// Conecta os eventos de combate às animações do golem inimigo.
//   - Padrão: idle (cristais pulsando)
//   - Ao atacar o jogador: ataque/lunge (uma vez, volta pro idle)
//   - Ao levar dano do jogador: dano/recoil (uma vez, volta pro idle)
//   - Ao ser derrotado: colapso (uma vez, congela no último frame)
//   - Novo andar → detecta a troca de inimigo e carrega a variante
//     do golem do elemento certo (EnemySpriteLibrary).
//
// Coloque este script no MESMO objeto que tem o SpriteAnimator do inimigo.

using System.Collections;
using UnityEngine;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    [RequireComponent(typeof(SpriteAnimator))]
    public class EnemyAnimationController : MonoBehaviour
    {
        [Header("Nomes das animações (devem bater com o SpriteAnimator)")]
        [SerializeField] private string idleAnim    = "idle";
        [SerializeField] private string ataqueAnim  = "ataque";
        [SerializeField] private string danoAnim    = "dano";
        [SerializeField] private string colapsoAnim = "colapso";

        private SpriteAnimator _animator;
        private string _loadedEnemyName;

        private void Awake()
        {
            _animator = GetComponent<SpriteAnimator>();
        }

        private void Start()
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnStateChanged  += OnStateChanged;
                CombatManager.Instance.OnDamageApplied += OnDamageApplied;
                CombatManager.Instance.OnCombatEnded   += OnCombatEnded;
            }

            RefreshVariantIfNeeded();
        }

        private void OnDestroy()
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnStateChanged  -= OnStateChanged;
                CombatManager.Instance.OnDamageApplied -= OnDamageApplied;
                CombatManager.Instance.OnCombatEnded   -= OnCombatEnded;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // REAÇÃO AOS EVENTOS
        // ─────────────────────────────────────────────────────────────────────

        private void OnStateChanged(CombatState state)
        {
            // Novo combate/andar pode ter trocado o inimigo
            RefreshVariantIfNeeded();

            switch (state)
            {
                case CombatState.PLAYER_TURN:
                    // Não sobrepõe one-shots em andamento (mesmo guard do jogador:
                    // a transição de estado chega na mesma pilha do evento de dano)
                    if (_animator.CurrentAnimation == danoAnim ||
                        _animator.CurrentAnimation == ataqueAnim ||
                        _animator.CurrentAnimation == colapsoAnim)
                        break;
                    _animator.Play(idleAnim);
                    break;
            }
        }

        private void OnDamageApplied(DamageResult result, bool isPlayerAttacking)
        {
            if (isPlayerAttacking)
                StartCoroutine(RecoilOnImpact());         // espera o projétil chegar
            else
                _animator.PlayOnce(ataqueAnim, idleAnim); // golem golpeando o jogador
        }

        /// <summary>
        /// O dano é aplicado na hora, mas o projétil leva um instante para cruzar a
        /// arena — o recuo espera o impacto para não reagir antes da magia chegar.
        /// </summary>
        private IEnumerator RecoilOnImpact()
        {
            yield return new WaitForSeconds(SpellVFXPlayer.FlightTime);

            // Se o golem morreu nesse meio-tempo, o colapso tem prioridade
            if (_animator.CurrentAnimation == colapsoAnim) yield break;

            _animator.PlayOnce(danoAnim, idleAnim);
        }

        private void OnCombatEnded(bool playerWon)
        {
            // Derrotado → colapsa e congela no último frame (loop=false)
            if (playerWon)
                _animator.Play(colapsoAnim, force: true);
        }

        // ─────────────────────────────────────────────────────────────────────
        // TROCA DE VARIANTE (um golem por andar)
        // ─────────────────────────────────────────────────────────────────────

        private void RefreshVariantIfNeeded()
        {
            var enemy = CombatManager.Instance != null ? CombatManager.Instance.CurrentEnemy : null;

            // Sem combate ainda: carrega a variante padrão para não ficar invisível
            string key = enemy != null ? enemy.Nome : "(default)";
            if (key == _loadedEnemyName) return;
            _loadedEnemyName = key;

            _animator.SetAnimations(EnemySpriteLibrary.LoadAnimations(enemy != null ? enemy.Elemento : null));
            _animator.Play(idleAnim, force: true);
        }
    }
}
