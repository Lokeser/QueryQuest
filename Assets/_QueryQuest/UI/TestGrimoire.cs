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
            if (combat.CurrentState == CombatState.PLAYER_TURN)
                combat.OpenGrimoire();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (combat.CurrentState == CombatState.GRIMOIRE_OPEN)
                combat.CloseGrimoire();
        }
    }
}