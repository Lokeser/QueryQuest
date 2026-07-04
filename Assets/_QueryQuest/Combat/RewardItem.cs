// Assets/_QueryQuest/Combat/RewardItem.cs
// Representa um item de recompensa oferecido após vencer um andar.

namespace QueryQuest.Combat
{
    public enum RewardType
    {
        Armor,   // Armadura — reduz dano recebido
        Staff,   // Cajado elemental — aumenta dano de 1 elemento
        Page     // Página de magia — desbloqueia nova magia
    }

    public class RewardItem
    {
        public RewardType Type;
        public string Title;        // Nome exibido
        public string Description;  // Descrição do efeito
        public string Element;      // Para Staff: qual elemento
        public int SpellId;         // Para Page: qual magia desbloquear
        public string SpellName;    // Para Page: nome da magia

        public override string ToString() => $"{Title} — {Description}";
    }
}
