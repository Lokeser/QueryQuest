// Assets/_QueryQuest/UI/SpiritUI.cs
// Balão de fala do espírito companheiro (placeholder visual).
// Fica acima do herói na arena. Mostra a dica quando o espírito fala,
// e some depois de alguns segundos.
//
// Hierarquia esperada:
//
// SpiritBubble (este script)  [Image = balão, começa invisível]
// └── SpiritText              (TMP)

using System.Collections;
using UnityEngine;
using TMPro;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    public class SpiritUI : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private GameObject bubble;       // o balão (liga/desliga)
        [SerializeField] private TextMeshProUGUI bubbleText;

        [Header("Configuração")]
        [SerializeField] private float displayDuration = 6f;

        private Coroutine _hideRoutine;

        private void Start()
        {
            if (SpiritCompanion.Instance != null)
                SpiritCompanion.Instance.OnSpiritSpeak += Speak;

            HideBubble();
        }

        private void OnDestroy()
        {
            if (SpiritCompanion.Instance != null)
                SpiritCompanion.Instance.OnSpiritSpeak -= Speak;
        }

        public void Speak(string message)
        {
            if (bubbleText != null) bubbleText.text = message;
            ShowBubble();

            if (_hideRoutine != null) StopCoroutine(_hideRoutine);
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displayDuration);
            HideBubble();
        }

        private void ShowBubble()
        {
            if (bubble != null) bubble.SetActive(true);
            else gameObject.SetActive(true);
        }

        private void HideBubble()
        {
            if (bubble != null) bubble.SetActive(false);
        }
    }
}
