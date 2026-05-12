// Assets/_QueryQuest/Models/SpellData.cs
using SQLite;

namespace QueryQuest.Models
{
    /// <summary>
    /// Representa um registro da tabela "Feiticos" no banco SQLite.
    /// Cada instância = uma linha da tabela.
    /// </summary>
    [Table("Feiticos")]
    public class SpellData
    {
        [PrimaryKey, AutoIncrement]
        [Column("Id")]
        public int Id { get; set; }

        [NotNull]
        [Column("Nome")]
        public string Nome { get; set; }

        [NotNull]
        [Column("Elemento")]
        public string Elemento { get; set; }  // Fogo | Agua | Vento | Terra | Raio

        [NotNull]
        [Column("Nivel")]
        public int Nivel { get; set; }         // 1, 2, 3...

        [NotNull]
        [Column("Distancia")]
        public string Distancia { get; set; }  // CURTO | MEDIO | LONGO

        [NotNull]
        [Column("DanoBase")]
        public int DanoBase { get; set; }

        [Column("Descricao")]
        public string Descricao { get; set; }

        [Column("Desbloqueado")]
        public int Desbloqueado { get; set; } = 0; // 0 = false, 1 = true (SQLite bool)

        public override string ToString()
        {
            return $"[{Id}] {Nome} | {Elemento} | Nv.{Nivel} | {Distancia} | {DanoBase} dmg";
        }
    }
}
