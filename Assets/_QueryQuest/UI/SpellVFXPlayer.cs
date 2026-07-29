// Assets/_QueryQuest/UI/SpellVFXPlayer.cs
// Toca o VFX de uma magia: um projétil que voa do herói até o alvo e,
// na chegada, a explosão.
//
//   COR    = elemento da magia (os sheets são cinza e recebem o tint)
//   TAMANHO= nível da magia (0.75x nv1, 1.2x nv2, 1.8x nv3)
//
// Criado por código pelo ArenaSceneView; os objetos de VFX são temporários
// (destruídos ao terminar a animação).

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace QueryQuest.UI
{
    public class SpellVFXPlayer : MonoBehaviour
    {
        /// <summary>Tempo de voo do projétil. O recuo do inimigo espera isso para bater com o impacto.</summary>
        public const float FlightTime = 0.35f;

        private const float ProjectileFps = 24f;
        private const float ExplosionFps  = 42f;

        [Tooltip("Tamanho do VFX em nível 1x, antes da escala por nível.")]
        [SerializeField] private Vector2 baseSize = new Vector2(170f, 200f);

        private Sprite[] _projectile;
        private Sprite[] _explosion;

        private void Awake()
        {
            _projectile = SpriteSheetLoader.Load("Sprites/VFX/projetil_sheet");
            _explosion  = SpriteSheetLoader.Load("Sprites/VFX/explosao_sheet");
        }

        public bool HasSprites => _projectile.Length > 0 && _explosion.Length > 0;

        /// <summary>Dispara o projétil de 'from' até 'to' (coordenadas locais deste transform).</summary>
        public void Play(Vector2 from, Vector2 to, string elemento, int nivel)
        {
            if (!HasSprites) return;
            StartCoroutine(PlayRoutine(from, to, ElementPalette.For(elemento),
                                       ElementPalette.ScaleForLevel(nivel)));
        }

        private IEnumerator PlayRoutine(Vector2 from, Vector2 to, Color color, float scale)
        {
            Vector2 size = baseSize * scale;

            // ── Projétil ────────────────────────────────────────────────────
            var proj = CreateVFXObject("SpellProjectile", size, color);
            var projRT  = proj.rectTransform;
            var projImg = proj;

            // O sheet foi desenhado voando para a ESQUERDA; espelha se o alvo está à direita
            float dir = Mathf.Sign(to.x - from.x);
            if (dir == 0f) dir = 1f;
            projRT.localScale = new Vector3(-dir, 1f, 1f);

            float t = 0f;
            int frame = 0;
            float frameTimer = 0f;
            projRT.anchoredPosition = from;

            while (t < FlightTime)
            {
                t += Time.deltaTime;
                projRT.anchoredPosition = Vector2.Lerp(from, to, Mathf.Clamp01(t / FlightTime));

                frameTimer += Time.deltaTime;
                if (frameTimer >= 1f / ProjectileFps)
                {
                    frameTimer = 0f;
                    frame = (frame + 1) % _projectile.Length;
                    projImg.sprite = _projectile[frame];
                }
                yield return null;
            }
            Destroy(proj.gameObject);

            // ── Explosão ────────────────────────────────────────────────────
            var boom = CreateVFXObject("SpellExplosion", size * 1.4f, color);
            boom.rectTransform.anchoredPosition = to;

            float boomFrameTime = 1f / ExplosionFps;
            for (int i = 0; i < _explosion.Length; i++)
            {
                boom.sprite = _explosion[i];
                yield return new WaitForSeconds(boomFrameTime);
            }
            Destroy(boom.gameObject);
        }

        private Image CreateVFXObject(string name, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = color;                 // tint sobre o sheet cinza = cor do elemento
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }
    }
}
