// Assets/_QueryQuest/UI/DraggablePanel.cs
// Torna um painel arrastável pela "alça" (geralmente o Header).
// Não redimensiona — apenas move.
//
// Como usar:
//   1. Adicione este script no painel que quer mover (ex: GrimoirePanel)
//   2. Arraste o objeto da "alça" (ex: Header) para o campo Drag Handle
//   3. Se Drag Handle ficar vazio, o painel inteiro vira a área de arrasto

using UnityEngine;
using UnityEngine.EventSystems;

namespace QueryQuest.UI
{
    public class DraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [Header("Alça de Arrasto")]
        [Tooltip("Objeto pelo qual o painel é arrastado (ex: Header). Se vazio, usa o painel inteiro.")]
        [SerializeField] private RectTransform dragHandle;

        [Header("Limites")]
        [Tooltip("Mantém o painel dentro da tela")]
        [SerializeField] private bool clampToScreen = true;

        private RectTransform _panel;
        private Canvas _canvas;
        private Vector2 _pointerOffset;

        private void Awake()
        {
            _panel = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();

            // Se uma alça foi definida, adiciona o listener nela
            if (dragHandle != null)
            {
                var trigger = dragHandle.gameObject.GetComponent<DragHandleForwarder>();
                if (trigger == null)
                    trigger = dragHandle.gameObject.AddComponent<DragHandleForwarder>();
                trigger.Init(this);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _panel, eventData.position, eventData.pressEventCamera, out _pointerOffset);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_canvas == null) return;

            Vector2 localPoint;
            RectTransform canvasRect = _canvas.transform as RectTransform;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                _panel.localPosition = localPoint - _pointerOffset;

                if (clampToScreen)
                    ClampToCanvas(canvasRect);
            }
        }

        private void ClampToCanvas(RectTransform canvasRect)
        {
            Vector3 pos = _panel.localPosition;

            float canvasW = canvasRect.rect.width;
            float canvasH = canvasRect.rect.height;
            float panelW = _panel.rect.width;
            float panelH = _panel.rect.height;

            float minX = -canvasW / 2f + panelW / 2f;
            float maxX =  canvasW / 2f - panelW / 2f;
            float minY = -canvasH / 2f + panelH / 2f;
            float maxY =  canvasH / 2f - panelH / 2f;

            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

            _panel.localPosition = pos;
        }

        // Chamado pela alça quando ela recebe o drag
        public void ForwardBeginDrag(PointerEventData e) => OnBeginDrag(e);
        public void ForwardDrag(PointerEventData e) => OnDrag(e);
    }

    /// <summary>
    /// Componente auxiliar adicionado à alça (Header) para redirecionar
    /// os eventos de drag para o DraggablePanel pai.
    /// </summary>
    public class DragHandleForwarder : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private DraggablePanel _target;
        public void Init(DraggablePanel target) => _target = target;

        public void OnBeginDrag(PointerEventData eventData) => _target?.ForwardBeginDrag(eventData);
        public void OnDrag(PointerEventData eventData) => _target?.ForwardDrag(eventData);
    }
}
