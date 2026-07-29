// Assets/_QueryQuest/Models/FragmentoData.cs
// Um fragmento de dado corrompido que o golem deixa cair ao ser derrotado.
// A tabela Fragmentos se liga a Inimigos por InimigoID — é essa relação que
// a magia "Inspecionar Fragmento" ensina a percorrer com JOIN.

using SQLite;

namespace QueryQuest.Models
{
    [Table("Fragmentos")]
    public class FragmentoData
    {
        [PrimaryKey]
        [Column("FragmentoID")]
        public int FragmentoID { get; set; }

        [NotNull]
        [Column("InimigoID")]
        public int InimigoID { get; set; }      // FK -> Inimigos.Id

        [NotNull]
        [Column("Nome")]
        public string Nome { get; set; }

        [Column("Elemento")]
        public string Elemento { get; set; }    // Fogo | Agua | Vento | Terra | Raio | Neutro

        [Column("Tipo")]
        public string Tipo { get; set; }        // Ofensivo | Defensivo | Raro | Utilitario

        [Column("Raridade")]
        public int Raridade { get; set; }       // 1 = comum ... 5 = lendario

        [Column("Descricao")]
        public string Descricao { get; set; }

        [Column("ValorXP")]
        public int ValorXP { get; set; }

        [Column("Pista")]
        public string Pista { get; set; }       // dica estrategica sobre o golem que largou

        public override string ToString()
            => $"[{FragmentoID}] {Nome} | {Elemento} | {Tipo} | R{Raridade}";
    }
}
