using UnityEngine;

namespace Hamster.Village
{
    // Fixed camera that frames the whole village map; it never follows the player.
    // Refits only when the screen size changes (rotation, Simulator device switch).
    [RequireComponent(typeof(Camera))]
    public class MapCamera : MonoBehaviour
    {
        public Vector2 mapSize = new Vector2(18f, 26f);
        public Vector2 mapCenter;

        Camera cam;
        Vector2Int screen;

        void Awake() => cam = GetComponent<Camera>();

        void LateUpdate() => Fit(false);

        public void Fit(bool force)
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (!force && Screen.width == screen.x && Screen.height == screen.y) return;
            screen = new Vector2Int(Screen.width, Screen.height);
            // Show the whole map: tall phones are limited by width, wide screens by height.
            cam.orthographicSize = Mathf.Max(mapSize.y / 2f, mapSize.x / 2f / cam.aspect);
            transform.position = new Vector3(mapCenter.x, mapCenter.y, transform.position.z);
        }
    }
}
