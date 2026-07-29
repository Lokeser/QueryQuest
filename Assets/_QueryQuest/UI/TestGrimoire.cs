using UnityEngine;
using UnityEngine.InputSystem;
using QueryQuest.Combat;

public class TestGrimoire : MonoBehaviour
{
    void Update()
    {
        var combat = CombatManager.Instance;
        if (combat == null) return;

        // Sem teclado (batch mode, nographics) Keyboard.current e null
        var teclado = Keyboard.current;
        if (teclado == null) return;

        if (teclado.gKey.wasPressedThisFrame)
        {
            if (combat.CurrentState == CombatState.PLAYER_TURN)
                combat.OpenGrimoire();
        }

        if (teclado.escapeKey.wasPressedThisFrame)
        {
            if (combat.CurrentState == CombatState.GRIMOIRE_OPEN)
                combat.CloseGrimoire();
        }
    }
}