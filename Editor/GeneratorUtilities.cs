using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CrystalCollector.EditorTools
{
    internal static class GeneratorUtilities
    {
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folderName = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent)) AssetDatabase.CreateFolder(parent, folderName);
        }

        public static Material CreateMaterial(string assetPath, Color color, float metallic, float smoothness, Color emission, float emissionIntensity)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(assetPath) };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", emission * emissionIntensity);
                material.EnableKeyword("_EMISSION");
            }
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        public static GameObject CreatePrimitive(PrimitiveType primitiveType, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider = false)
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;
            if (material != null && primitive.TryGetComponent<Renderer>(out var renderer)) renderer.sharedMaterial = material;
            if (!keepCollider && primitive.TryGetComponent<Collider>(out var collider)) Object.DestroyImmediate(collider);
            return primitive;
        }

        public static Canvas CreateCanvas(string name, out EventSystem eventSystem)
        {
            var canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem = eventObject.GetComponent<EventSystem>();
            return canvas;
        }

        public static Image CreateImage(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var rect = imageObject.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), anchoredPosition, sizeDelta);
            var image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text CreateText(string name, Transform parent, string content, int fontSize, Color color, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), anchoredPosition, sizeDelta);
            var text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 sizeDelta, Color accent)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, sizeDelta);
            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.07f, 0.1f, 0.18f, 0.95f);
            var button = buttonObject.GetComponent<Button>();
            var feedback = buttonObject.AddComponent<UIButtonFX>();
            feedback.Configure(accent);
            var colors = button.colors;
            colors.normalColor = new Color(0.08f, 0.11f, 0.2f, 0.95f);
            colors.highlightedColor = new Color(accent.r * 0.35f + 0.12f, accent.g * 0.35f + 0.12f, accent.b * 0.35f + 0.12f, 1f);
            colors.pressedColor = accent;
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.12f;
            button.colors = colors;

            var labelText = CreateText("Label", buttonObject.transform, label, 24, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            labelText.rectTransform.offsetMin = Vector2.zero;
            labelText.rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        public static CanvasGroup AddCanvasGroup(GameObject target, bool visible)
        {
            var group = target.GetComponent<CanvasGroup>();
            if (group == null) group = target.AddComponent<CanvasGroup>();
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            return group;
        }

        public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        public static void SetLookAt(Transform source, Vector3 target)
        {
            source.rotation = Quaternion.LookRotation(target - source.position, Vector3.up);
        }
    }
}
