using UnityEngine;

namespace Hamster.Village
{
    // Keeps a full-screen RectTransform inside Screen.safeArea (notch, Dynamic Island, home indicator,
    // Android camera cutouts). Only touches the layout when the safe area actually changes.
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        RectTransform rt;
        Rect applied;
        Vector2Int screen;

        void Awake()
        {
            rt = GetComponent<RectTransform>();
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != applied || Screen.width != screen.x || Screen.height != screen.y) Apply();
        }

        void Apply()
        {
            applied = Screen.safeArea;
            screen = new Vector2Int(Screen.width, Screen.height);
            if (screen.x <= 0 || screen.y <= 0) return;
            rt.anchorMin = new Vector2(applied.xMin / screen.x, applied.yMin / screen.y);
            rt.anchorMax = new Vector2(applied.xMax / screen.x, applied.yMax / screen.y);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
