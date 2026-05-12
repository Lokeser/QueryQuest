// Assets/_QueryQuest/Combat/CombatState.cs
namespace QueryQuest.Combat
{
    /// <summary>
    /// Todos os estados possíveis da máquina de estados do combate.
    /// </summary>
    public enum CombatState
    {
        IDLE,               // Fora de combate
        PLAYER_TURN,        // Aguardando ação do jogador (abrir grimório, fugir, mudar distância)
        GRIMOIRE_OPEN,      // Grimório aberto — jogador digita SQL
        QUERY_EXECUTING,    // Query sendo processada pelo SQLInterpreter
        SPELL_CAST,         // Feitiço selecionado, animação/efeito sendo aplicado
        APPLYING_DAMAGE,    // Dano sendo calculado e subtraído do HP
        ENEMY_TURN,         // IA do inimigo executando ataque
        CHECKING_RESULT,    // Verificando se alguém morreu ou combate continua
    }
}
