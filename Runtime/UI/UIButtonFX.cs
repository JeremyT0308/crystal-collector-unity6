using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CrystalCollector
{
    /// <summary>
    /// Lightweight generated button feedback that preserves the Button's normal event flow.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public sealed class UIButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Color accent = new Color(0.1f, 0.82f, 1f);
        [SerializeField] private float hoverScale = 1.035f;

        private RectTransform rectTransform;
        private Image image;
        private Vector3 baseScale;
        private Color baseColor;
        private bool highlighted;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            image = GetComponent<Image>();
            baseScale = transform.localScale;
            baseColor = image == null ? Color.white : image.color;
        }

        private void Update()
        {
            if (rectTransform == null) return;
            var targetScale = highlighted ? baseScale * hoverScale : baseScale;
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, 14f * Time.unscaledDeltaTime);
            if (image != null)
            {
                var targetColor = highlighted ? Color.Lerp(baseColor, accent, 0.22f) : baseColor;
                image.color = Color.Lerp(image.color, targetColor, 14f * Time.unscaledDeltaTime);
            }
        }

        public void Configure(Color newAccent)
        {
            accent = newAccent;
        }

        public void OnPointerEnter(PointerEventData eventData) => SetHighlighted(true);
        public void OnPointerExit(PointerEventData eventData) => SetHighlighted(false);
        public void OnPointerDown(PointerEventData eventData) => SetHighlighted(true);
        public void OnPointerUp(PointerEventData eventData) => SetHighlighted(true);
        public void OnSelect(BaseEventData eventData) => SetHighlighted(true);
        public void OnDeselect(BaseEventData eventData) => SetHighlighted(false);

        private void SetHighlighted(bool value)
        {
            highlighted = value;
        }
    }
}
