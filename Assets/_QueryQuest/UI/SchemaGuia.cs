// Assets/_QueryQuest/UI/SchemaGuia.cs
// Descrição didática das tabelas do banco, lida do PRÓPRIO esquema em runtime.
//
// A lista de colunas vem de PRAGMA table_info, não de uma cópia escrita à mão:
// se o banco mudar, a explicação muda junto e nunca fica mentindo para o aluno.
// O que está escrito aqui é só o texto que acompanha cada coluna.

using System.Collections.Generic;
using System.Text;
using QueryQuest.Database;

namespace QueryQuest.UI
{
    public static class SchemaGuia
    {
        /// <summary>Coluna do banco com a explicação e o peso didático.</summary>
        public struct Coluna
        {
            public string Nome;
            public string Tipo;
            public string Explicacao;
            public bool Destaque;   // as que decidem a luta
        }

        // Texto de apoio por tabela.coluna. O que não estiver aqui aparece assim
        // mesmo, com a descrição vazia — melhor do que sumir da listagem.
        private static readonly Dictionary<string, (string txt, bool destaque)> Textos =
            new Dictionary<string, (string, bool)>
        {
            // ── Magias ───────────────────────────────────────────────────────
            ["Magias.Id"]           = ("Identificador único da magia.", false),
            ["Magias.Nome"]         = ("Nome da magia. É texto: no WHERE precisa de aspas simples, como 'Rajada'.", true),
            ["Magias.Elemento"]     = ("Fogo, Agua, Vento, Terra ou Raio. Case com a fraqueza do golem para dobrar o estrago.", true),
            ["Magias.Nivel"]        = ("1, 2 ou 3. Nível 1 você já tem; nível 2 vem do JOIN no fragmento.", false),
            ["Magias.Distancia"]    = ("CURTO, MEDIO ou LONGO. Define de quantos slots de distância a magia alcança.", true),
            ["Magias.DanoBase"]     = ("Dano antes dos bônus de elemento e de distância. É número: no WHERE vai sem aspas.", true),
            ["Magias.Descricao"]    = ("Texto livre. Não influencia o combate.", false),
            ["Magias.Desbloqueado"] = ("1 = você já domina a magia, 0 = ainda não. Consultar uma magia bloqueada não a lança.", false),

            // ── Inimigos ─────────────────────────────────────────────────────
            ["Inimigos.Id"]                = ("Identificador do golem. É esta coluna que o fragmento aponta no JOIN.", true),
            ["Inimigos.Nome"]              = ("Nome do golem.", false),
            ["Inimigos.Elemento"]          = ("O elemento DELE. Não confunda com a fraqueza: atacar um golem de Fogo com Fogo não ajuda.", true),
            ["Inimigos.HP"]                = ("Pontos de vida totais.", false),
            ["Inimigos.Nivel"]             = ("Nível do golem. Quanto maior, mais forte o golpe dele.", false),
            ["Inimigos.FraquezaElemento"]  = ("O elemento que fere este golem. É a informação mais valiosa da tabela.", true),
            ["Inimigos.AtaqueDistancia"]   = ("De onde ele ataca. Fora desse alcance, o golpe dele erra.", true),
            ["Inimigos.FraquezaDistancia"] = ("A distância em que ele sofre dano EXTRA. Posicione-se nela.", true),
            ["Inimigos.Descricao"]         = ("Texto livre. Não influencia o combate.", false),

            // ── Fragmentos ───────────────────────────────────────────────────
            ["Fragmentos.FragmentoID"] = ("Identificador do fragmento. É o que você filtra no WHERE ao absorver.", true),
            ["Fragmentos.InimigoID"]   = ("Chave estrangeira: aponta para Inimigos.Id. É a ponte do JOIN.", true),
            ["Fragmentos.Nome"]        = ("Nome do fragmento.", false),
            ["Fragmentos.Elemento"]    = ("Elemento da essência. Define qual magia de nível 2 você ganha ao absorver.", true),
            ["Fragmentos.Tipo"]        = ("Categoria do fragmento.", false),
            ["Fragmentos.Raridade"]    = ("De 1 (comum) a 5 (lendário).", false),
            ["Fragmentos.Descricao"]   = ("Texto livre.", false),
            ["Fragmentos.ValorXP"]     = ("Valor de experiência do fragmento.", false),
            ["Fragmentos.Pista"]       = ("Dica estratégica sobre o golem que largou o fragmento.", false),
        };

        /// <summary>
        /// Linha de "pragma table_info". O ColumnInfo do SQLite embarcado só traz
        /// o nome (a coluna de tipo está comentada na biblioteca), e o tipo importa
        /// aqui: é ele que diz ao aluno se o valor vai entre aspas ou não.
        /// </summary>
        private class LinhaPragma
        {
            [SQLite.Column("name")] public string Name { get; set; }
            [SQLite.Column("type")] public string Type { get; set; }
        }

        /// <summary>Colunas reais da tabela, direto do esquema do banco.</summary>
        public static List<Coluna> Colunas(string tabela)
        {
            var saida = new List<Coluna>();
            var db = DatabaseManager.Instance?.DB;
            if (db == null) return saida;

            List<LinhaPragma> info;
            try { info = db.Query<LinhaPragma>($"pragma table_info(\"{tabela}\")"); }
            catch { return saida; }
            if (info == null) return saida;

            foreach (var c in info)
            {
                Textos.TryGetValue($"{tabela}.{c.Name}", out var t);
                saida.Add(new Coluna
                {
                    Nome = c.Name,
                    Tipo = string.IsNullOrEmpty(c.Type) ? "" : c.Type.ToLowerInvariant(),
                    Explicacao = t.txt ?? "",
                    Destaque = t.destaque,
                });
            }
            return saida;
        }

        /// <summary>
        /// Uma linha só com os nomes das colunas, para ficar ao lado da HUD.
        /// As colunas que decidem a luta vêm destacadas.
        /// </summary>
        public static string LinhaDeColunas(string tabela, string corDestaque = "#F0C64A")
        {
            var colunas = Colunas(tabela);
            if (colunas.Count == 0) return "";

            var sb = new StringBuilder();
            sb.Append($"<b>{tabela}</b>\n");
            for (int i = 0; i < colunas.Count; i++)
            {
                if (i > 0) sb.Append(" · ");
                if (colunas[i].Destaque) sb.Append($"<color={corDestaque}><b>{colunas[i].Nome}</b></color>");
                else sb.Append(colunas[i].Nome);
            }
            return sb.ToString();
        }
    }
}
