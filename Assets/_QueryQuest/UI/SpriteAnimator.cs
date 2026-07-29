// Assets/_QueryQuest/UI/SpriteAnimator.cs
// Animador de sprites por frames para um Image da UI.
// Toca sequências de sprites (fatiadas de um spritesheet) com controle de FPS,
// loop, e callback ao terminar. Feito para funcionar no Canvas (UI).
//
// As animações podem ser configuradas no Inspector OU injetadas por código
// via SetAnimations() (é assim que o ArenaSceneView monta o protagonista).

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace QueryQuest.UI
{
    [Serializable]
    public class SpriteAnimation
    {
        public string name;
        public Sprite[] frames;
        public float fps = 12f;
        public bool loop = true;
        [Tooltip("Espelha horizontalmente ao tocar esta animação (para sheets desenhados olhando para o lado oposto).")]
        public bool flipX = false;
    }

    [RequireComponent(typeof(Image))]
    public class SpriteAnimator : MonoBehaviour
    {
        [Header("Animações")]
        [SerializeField] private SpriteAnimation[] animations;

        [Header("Configuração")]
        [SerializeField] private string defaultAnimation = "idle";
        [SerializeField] private bool preserveAspect = true;

        private Image _image;
        private Coroutine _current;
        private string _currentName;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _image.preserveAspect = preserveAspect;
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(defaultAnimation))
                Play(defaultAnimation);
        }

        /// <summary>Define as animações por código (substitui as do Inspector).</summary>
        public void SetAnimations(SpriteAnimation[] anims)
        {
            animations = anims;
        }

        /// <summary>Toca uma animação pelo nome. Se já estiver tocando, ignora (a menos que force).</summary>
        public void Play(string animName, bool force = false, Action onComplete = null)
        {
            if (!force && _currentName == animName) return;

            var anim = FindAnimation(animName);
            if (anim == null || anim.frames == null || anim.frames.Length == 0)
            {
                Debug.LogWarning($"[SpriteAnimator] Animação '{animName}' não encontrada ou vazia.");
                return;
            }

            if (_current != null) StopCoroutine(_current);
            _currentName = animName;
            ApplyFlip(anim);
            _current = StartCoroutine(PlayRoutine(anim, onComplete));
        }

        /// <summary>Toca uma animação uma vez e volta para a default ao terminar.</summary>
        public void PlayOnce(string animName, string returnTo = null)
        {
            var anim = FindAnimation(animName);
            if (anim == null) return;

            // Força tocar sem loop, e ao terminar volta para returnTo (ou default)
            if (_current != null) StopCoroutine(_current);
            _currentName = animName;
            ApplyFlip(anim);
            _current = StartCoroutine(PlayOnceRoutine(anim, returnTo ?? defaultAnimation));
        }

        /// <summary>Espelha (ou não) o transform conforme a animação pede.</summary>
        private void ApplyFlip(SpriteAnimation anim)
        {
            var s = transform.localScale;
            float x = Mathf.Abs(s.x) * (anim.flipX ? -1f : 1f);
            transform.localScale = new Vector3(x, s.y, s.z);
        }

        private IEnumerator PlayRoutine(SpriteAnimation anim, Action onComplete)
        {
            float frameTime = 1f / Mathf.Max(1f, anim.fps);
            int i = 0;

            while (true)
            {
                _image.sprite = anim.frames[i];
                yield return new WaitForSeconds(frameTime);
                i++;

                if (i >= anim.frames.Length)
                {
                    if (anim.loop) i = 0;
                    else break;
                }
            }
            onComplete?.Invoke();
        }

        private IEnumerator PlayOnceRoutine(SpriteAnimation anim, string returnTo)
        {
            float frameTime = 1f / Mathf.Max(1f, anim.fps);
            for (int i = 0; i < anim.frames.Length; i++)
            {
                _image.sprite = anim.frames[i];
                yield return new WaitForSeconds(frameTime);
            }
            // Volta para a animação de retorno
            Play(returnTo, force: true);
        }

        private SpriteAnimation FindAnimation(string name)
        {
            if (animations == null) return null;
            foreach (var a in animations)
                if (a.name == name) return a;
            return null;
        }

        public string CurrentAnimation => _currentName;
    }
}
