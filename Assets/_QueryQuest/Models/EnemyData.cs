// Assets/_QueryQuest/Models/EnemyData.cs
using SQLite;

namespace QueryQuest.Models
{
    [Table("Inimigos")]
    public class EnemyData
    {
        [PrimaryKey, AutoIncrement]
        [Column("Id")]
        public int Id { get; set; }

        [NotNull]
        [Column("Nome")]
        public string Nome { get; set; }

        [NotNull]
        [Column("Elemento")]
        public string Elemento { get; set; }

        [NotNull]
        [Column("HP")]
        public int HP { get; set; }

        [NotNull]
        [Column("Nivel")]
        public int Nivel { get; set; }

        [Column("FraquezaElemento")]
        public string FraquezaElemento { get; set; } // calculado pelo ElementalSystem, mas guardado para consulta educacional

        [Column("Descricao")]
        public string Descricao { get; set; }
    }
}
