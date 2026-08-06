// Assets/_QueryQuest/Combat/GameSession.cs
// O pouco de estado que precisa atravessar a troca de cena (menu -> combate)
// e o progresso que sobrevive a fechar o jogo.

using UnityEngine;

namespace QueryQuest.Combat
{
    public static class GameSession
    {
        private const string ChaveAndar = "QQ_AndarSalvo";

        /// <summary>Ligado pelo menu ao começar um jogo novo (o combate abre o tutorial).</summary>
        public static bool MostrarTutorial;

        /// <summary>Andar em que o combate deve começar. 1 = jogo novo.</summary>
        public static int AndarInicial = 1;

        /// <summary>True se já existe um andar salvo (habilita o "Continuar").</summary>
        public static bool TemProgresso => PlayerPrefs.GetInt(ChaveAndar, 0) > 0;

        public static int AndarSalvo => Mathf.Max(1, PlayerPrefs.GetInt(ChaveAndar, 1));

        /// <summary>Guarda o andar alcançado — chamado pelo FloorManager a cada andar.</summary>
        public static void SalvarAndar(int andar)
        {
            PlayerPrefs.SetInt(ChaveAndar, andar);
            PlayerPrefs.Save();
        }

        public static void LimparProgresso()
        {
            PlayerPrefs.DeleteKey(ChaveAndar);
            PlayerPrefs.Save();
        }
    }
}
