// Assets/_QueryQuest/UI/PlayerAnimationController.cs
// Conecta os eventos de combate às animações do protagonista.
//   - Padrão: idle (respirando)
//   - Ao lançar magia: ataque (uma vez, volta pro idle)
//   - Ao tomar dano: dano (uma vez, volta pro idle)
//   - Ao mover: andando (rápido, volta pro idle)
//
// Coloque este script no MESMO objeto que tem o SpriteAnimator do jogador.

using UnityEngine;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    [RequireComponent(typeof(SpriteAnimator))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("Nomes das animações (devem bater com o SpriteAnimator)")]
        [SerializeField] private string idleAnim    = "idle";
        [SerializeField] private string grimorioAnim = "grimorio";
        [SerializeField] private string ataqueAnim  = "ataque";
        [SerializeField] private string danoAnim    = "dano";
        [SerializeField] private string andandoAnim = "andando";

        private SpriteAnimator _animator;
        private int _lastPlayerSlot = -1;

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
            }
            if (SlotSystem.Instance != null)
                SlotSystem.Instance.OnPositionsChanged += OnPositionsChanged;

            _animator.Play(idleAnim);
        }

        private void OnDestroy()
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnStateChanged  -= OnStateChanged;
                CombatManager.Instance.OnDamageApplied -= OnDamageApplied;
            }
            if (SlotSystem.Instance != null)
                SlotSystem.Instance.OnPositionsChanged -= OnPositionsChanged;
        }

        // ─────────────────────────────────────────────────────────────────────
        // REAÇÃO AOS EVENTOS
        // ─────────────────────────────────────────────────────────────────────

        private void OnStateChanged(CombatState state)
        {
            switch (state)
            {
                case CombatState.SPELL_CAST:
                    // Jogador lançou magia → animação de ataque
                    _animator.PlayOnce(ataqueAnim, idleAnim);
                    break;

                case CombatState.GRIMOIRE_OPEN:
                    // Abriu o grimório → pose de segurar o livro
                    _animator.Play(grimorioAnim);
                    break;

                case CombatState.PLAYER_TURN:
                    // O CombatManager transita para PLAYER_TURN na MESMA pilha em que
                    // dispara OnDamageApplied — sem este guard, o idle atropelaria a
                    // animação de dano na hora. One-shots voltam pro idle sozinhas.
                    if (_animator.CurrentAnimation == danoAnim ||
                        _animator.CurrentAnimation == ataqueAnim ||
                        _animator.CurrentAnimation == andandoAnim)
                        break;
                    _animator.Play(idleAnim);
                    break;
            }
        }

        private void OnDamageApplied(DamageResult result, bool isPlayerAttacking)
        {
            // Se o dano foi no jogador (inimigo atacando), toca animação de dano
            if (!isPlayerAttacking)
                _animator.PlayOnce(danoAnim, idleAnim);
        }

        private void OnPositionsChanged(int playerSlot, int enemySlot)
        {
            // Detecta movimento do jogador → animação de andar
            if (_lastPlayerSlot != -1 && playerSlot != _lastPlayerSlot)
                _animator.PlayOnce(andandoAnim, idleAnim);

            _lastPlayerSlot = playerSlot;
        }
    }
}
