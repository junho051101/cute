using System;
using Hamster.Art;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Hamster.Village
{
    // Portrait touch UI: an "enter" button near doors and a placeholder building panel.
    public class VillageUI : MonoBehaviour
    {
        public RectTransform Root { get; private set; }
        // Buttons and HUD go here so notches and the home indicator never cover them.
        public RectTransform SafeArea { get; private set; }
        public bool PanelOpen => panel != null && panel.activeSelf;
        public event Action<BuildingData> Entered;

        static readonly Color32 Ink = PixelArtLibrary.Ink;
        static readonly Color32 Cream = PixelArtLibrary.Hex("FFF8F2");
        static readonly Color32 Lilac = PixelArtLibrary.Hex("C9A9E8");
        static readonly Color32 Pink = PixelArtLibrary.Hex("FFD6E6");

        Font font;
        GameObject prompt;
        Text promptLabel;
        GameObject panel;
        Text panelTitle;
        Text panelBody;
        BuildingData promptTarget;

        public void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.transform.SetParent(transform, false);
            }

            var canvasGo = new GameObject("VillageCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            // Portrait: lock UI to screen width so taller phones only gain vertical room.
            scaler.matchWidthOrHeight = 0f;
            Root = canvasGo.GetComponent<RectTransform>();

            var safe = new GameObject("SafeArea", typeof(RectTransform));
            SafeArea = safe.GetComponent<RectTransform>();
            SafeArea.SetParent(Root, false);
            safe.AddComponent<SafeAreaFitter>();

            BuildPrompt();
            BuildPanel();
        }

        void BuildPrompt()
        {
            prompt = MakeButton("EnterButton", SafeArea, new Vector2(620f, 150f), OnEnterPressed, out promptLabel, 52);
            var rt = prompt.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 260f);
            // Own canvas so its per-frame bobbing only rebuilds this button, not the whole UI.
            prompt.AddComponent<Canvas>();
            prompt.AddComponent<GraphicRaycaster>();
            var bob = prompt.AddComponent<Bobbing>();
            bob.amplitude = 8f;
            bob.snap = 0f;
            prompt.SetActive(false);
        }

        void BuildPanel()
        {
            panel = new GameObject("BuildingPanel", typeof(RectTransform));
            var prt = panel.GetComponent<RectTransform>();
            prt.SetParent(Root, false);
            Stretch(prt);
            var dim = panel.AddComponent<Image>();
            dim.color = new Color(0.55f, 0.42f, 0.7f, 0.35f);

            var card = MakeImage("Card", prt, PixelArtLibrary.RoundedRect(Cream, Lilac), Color.white);
            card.type = Image.Type.Sliced;
            card.pixelsPerUnitMultiplier = 0.25f;
            card.rectTransform.sizeDelta = new Vector2(880f, 880f);

            panelTitle = MakeText("Title", card.rectTransform, 68, FontStyle.Bold);
            var trt = panelTitle.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.sizeDelta = new Vector2(-80f, 120f);
            trt.anchoredPosition = new Vector2(0f, -110f);

            panelBody = MakeText("Body", card.rectTransform, 44, FontStyle.Normal);
            var brt = panelBody.rectTransform;
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.offsetMin = new Vector2(70f, 220f);
            brt.offsetMax = new Vector2(-70f, -210f);
            panelBody.alignment = TextAnchor.UpperCenter;

            var close = MakeButton("CloseButton", card.rectTransform, new Vector2(360f, 130f), ClosePanel, out var closeLabel, 48);
            closeLabel.text = "닫기";
            var crt = close.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0f);
            crt.anchoredPosition = new Vector2(0f, 120f);

            panel.SetActive(false);
        }

        public void ShowPrompt(BuildingData b)
        {
            if (PanelOpen) return;
            if (promptTarget == b && prompt.activeSelf) return;
            promptTarget = b;
            promptLabel.text = $"{b.name} 들어가기";
            prompt.SetActive(true);
        }

        public void HidePrompt()
        {
            promptTarget = null;
            if (prompt.activeSelf) prompt.SetActive(false);
        }

        void OnEnterPressed()
        {
            if (promptTarget == null) return;
            var b = promptTarget;
            panelTitle.text = b.name;
            panelBody.text = b.description + "\n\n(준비 중)";
            prompt.SetActive(false);
            panel.SetActive(true);
            Entered?.Invoke(b);
        }

        void ClosePanel()
        {
            panel.SetActive(false);
            promptTarget = null;
        }

        GameObject MakeButton(string name, RectTransform parent, Vector2 size, Action onClick, out Text label, int fontSize)
        {
            var img = MakeImage(name, parent, PixelArtLibrary.RoundedRect(Pink, Lilac), Color.white);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.3f;
            img.rectTransform.sizeDelta = size;
            var btn = img.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            var colors = btn.colors;
            colors.pressedColor = new Color(0.9f, 0.85f, 1f);
            btn.colors = colors;
            label = MakeText("Label", img.rectTransform, fontSize, FontStyle.Bold);
            Stretch(label.rectTransform);
            label.raycastTarget = false;
            return img.gameObject;
        }

        Image MakeImage(string name, RectTransform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }

        Text MakeText(string name, RectTransform parent, int size, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = (Color)Ink;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
