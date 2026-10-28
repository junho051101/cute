using System.Collections.Generic;
using Hamster.Art;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hamster.Village
{
    // One-hand joystick: press anywhere that isn't a button, then drag.
    // WASD / arrow keys also work for testing on PC.
    public class FloatingJoystick : MonoBehaviour
    {
        public float radius = 130f;
        public Vector2 Value { get; private set; }
        public bool Locked { get; set; }

        RectTransform area;
        RectTransform baseRt;
        RectTransform knobRt;
        CanvasGroup group;
        bool dragging;
        Vector2 origin;
        readonly List<RaycastResult> hits = new List<RaycastResult>();

        public void Build(RectTransform canvasRoot)
        {
            area = canvasRoot;
            var baseGo = new GameObject("JoystickBase", typeof(RectTransform), typeof(CanvasGroup));
            baseRt = baseGo.GetComponent<RectTransform>();
            baseRt.SetParent(canvasRoot, false);
            baseRt.sizeDelta = Vector2.one * radius * 2f;
            group = baseGo.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            var baseImg = baseGo.AddComponent<Image>();
            baseImg.sprite = PixelArtLibrary.Circle(64, new Color32(255, 255, 255, 90), new Color32(201, 169, 232, 200));
            baseImg.raycastTarget = false;

            var knobGo = new GameObject("JoystickKnob", typeof(RectTransform));
            knobRt = knobGo.GetComponent<RectTransform>();
            knobRt.SetParent(baseRt, false);
            knobRt.sizeDelta = Vector2.one * radius * 0.9f;
            var knobImg = knobGo.AddComponent<Image>();
            knobImg.sprite = PixelArtLibrary.Circle(64, new Color32(255, 240, 250, 230), new Color32(201, 169, 232, 255));
            knobImg.raycastTarget = false;
        }

        void Update()
        {
            var stick = Vector2.zero;

            if (!Locked && ReadPointer(out var pos, out var began))
            {
                if (began && !dragging && !OverUI(pos))
                {
                    dragging = true;
                    origin = pos;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(area, pos, null, out var local);
                    baseRt.anchoredPosition = local;
                    group.alpha = 1f;
                }
                if (dragging)
                {
                    float scale = area.lossyScale.x > 0f ? area.lossyScale.x : 1f;
                    var delta = (pos - origin) / scale;
                    delta = Vector2.ClampMagnitude(delta, radius);
                    knobRt.anchoredPosition = delta;
                    stick = delta / radius;
                }
            }
            else
            {
                EndDrag();
            }

            if (!Locked) stick += ReadKeyboard();
            Value = Vector2.ClampMagnitude(stick, 1f);
        }

        void EndDrag()
        {
            dragging = false;
            if (group != null) group.alpha = 0f;
            if (knobRt != null) knobRt.anchoredPosition = Vector2.zero;
        }

        static bool ReadPointer(out Vector2 pos, out bool began)
        {
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                pos = touch.primaryTouch.position.ReadValue();
                began = touch.primaryTouch.press.wasPressedThisFrame;
                return true;
            }
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                pos = mouse.position.ReadValue();
                began = mouse.leftButton.wasPressedThisFrame;
                return true;
            }
            pos = default;
            began = false;
            return false;
        }

        static Vector2 ReadKeyboard()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;
            var v = Vector2.zero;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
            return v;
        }

        bool OverUI(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            hits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = screenPos }, hits);
            return hits.Count > 0;
        }
    }
}
