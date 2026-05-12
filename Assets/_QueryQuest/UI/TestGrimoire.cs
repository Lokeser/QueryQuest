using UnityEngine;
using UnityEngine.InputSystem;
using QueryQuest.Combat;

public class TestGrimoire : MonoBehaviour
{
    void Update()
    {
        var combat = CombatManager.Instance;
        if (combat == null) return;

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            // Só abre se for exatamente o turno do jogador
            if (combat.CurrentState == CombatState.PLAYER_TURN)
                combat.OpenGrimoire();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Só fecha se o grimório estiver aberto
            if (combat.CurrentState == CombatState.GRIMOIRE_OPEN)
                combat.CloseGrimoire();
        }
    }
}