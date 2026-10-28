using UnityEngine;

namespace Utopia.Title
{
    // Pixel-art idle: steps through a few fixed poses instead of tweening, like hand-drawn animation frames.
    // The transform is only touched when the frame changes.
    public class PixelIdle : MonoBehaviour
    {
        public Vector2[] scales = { Vector2.one };
        public float[] angles = { 0f };
        public float frameSeconds = 0.5f;

        int frame;
        float timer;

        void Start()
        {
            // Random start so neighbours (e.g. flowers) don't move in lockstep.
            timer = Random.Range(0f, frameSeconds);
            frame = Random.Range(0, FrameCount);
            Apply();
        }

        int FrameCount => Mathf.Max(scales.Length, angles.Length);

        void Update()
        {
            timer += Time.deltaTime;
            if (timer < frameSeconds) return;
            timer -= frameSeconds;
            frame = (frame + 1) % FrameCount;
            Apply();
        }

        void Apply()
        {
            var s = scales[frame % scales.Length];
            transform.localScale = new Vector3(s.x, s.y, 1f);
            transform.localRotation = Quaternion.Euler(0f, 0f, angles[frame % angles.Length]);
        }
    }

    // Every few seconds the hamster gives a tiny hop.
    public class PixelHop : MonoBehaviour
    {
        public float height = 0.12f;
        public Vector2 interval = new Vector2(2.5f, 4.5f);
        public float airSeconds = 0.16f;

        Vector3 home;
        float wait;
        float air = -1f;

        void Start()
        {
            home = transform.localPosition;
            wait = Random.Range(interval.x, interval.y);
        }

        void Update()
        {
            if (air >= 0f)
            {
                air += Time.deltaTime;
                if (air < airSeconds) return;
                air = -1f;
                transform.localPosition = home;
                wait = Random.Range(interval.x, interval.y);
                return;
            }
            wait -= Time.deltaTime;
            if (wait > 0f) return;
            air = 0f;
            transform.localPosition = home + new Vector3(0f, height, 0f);
        }
    }

    // Shows an overlay (closed eyes) for a moment at random intervals, sometimes twice in a row.
    public class Blinker : MonoBehaviour
    {
        public GameObject[] overlays;
        public Vector2 interval = new Vector2(2.2f, 4.5f);
        public float closedSeconds = 0.12f;

        float timer;
        bool closed;
        bool doubleBlink;

        void Start()
        {
            timer = Random.Range(interval.x, interval.y);
            Set(false);
        }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            if (closed)
            {
                Set(false);
                if (doubleBlink) { doubleBlink = false; timer = 0.14f; }
                else timer = Random.Range(interval.x, interval.y);
            }
            else
            {
                Set(true);
                if (!doubleBlink && Random.value < 0.25f) doubleBlink = true;
                timer = closedSeconds;
            }
        }

        void Set(bool on)
        {
            closed = on;
            foreach (var o in overlays) o.SetActive(on);
        }
    }

    // A sky sparkle that grows from a dot to a full star and back, then rests.
    public class Twinkle : MonoBehaviour
    {
        public Sprite[] frames;
        public float frameSeconds = 0.12f;
        public Vector2 rest = new Vector2(0.6f, 2.2f);

        SpriteRenderer sr;
        int step;
        float timer;
        // Ping-pong through the frames: small -> big -> small, then hidden while resting.
        int[] order;

        void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            order = new int[frames.Length * 2 - 1];
            for (int i = 0; i < frames.Length; i++) { order[i] = i; order[order.Length - 1 - i] = i; }
            step = Random.Range(0, order.Length);
            sr.sprite = frames[order[step]];
        }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            step++;
            if (step >= order.Length)
            {
                step = -1;
                sr.enabled = false;
                timer = Random.Range(rest.x, rest.y);
                return;
            }
            sr.enabled = true;
            sr.sprite = frames[order[step]];
            timer = frameSeconds;
        }
    }

    // Little light motes drifting upward one art-pixel at a time, flickering, then respawning lower down.
    public class FloatingMotes : MonoBehaviour
    {
        public Sprite sprite;
        public int count = 14;
        public Rect area;
        public float pixel = 0.06f;
        public float stepSeconds = 0.11f;
        public int sortingOrder = 45;
        public Color color = new Color(1f, 0.97f, 0.78f, 0.9f);

        SpriteRenderer[] motes;
        float[] life;
        float timer;

        void Start()
        {
            motes = new SpriteRenderer[count];
            life = new float[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Mote");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = color;
                sr.sortingOrder = sortingOrder;
                motes[i] = sr;
                Respawn(i, Random.Range(area.yMin, area.yMax));
            }
        }

        void Respawn(int i, float y)
        {
            float x = Random.Range(area.xMin, area.xMax);
            motes[i].transform.localPosition = new Vector3(Snap(x), Snap(y), 0f);
            life[i] = Random.Range(18, 40);
        }

        float Snap(float v) => Mathf.Round(v / pixel) * pixel;

        void Update()
        {
            timer += Time.deltaTime;
            if (timer < stepSeconds) return;
            timer -= stepSeconds;
            for (int i = 0; i < motes.Length; i++)
            {
                var t = motes[i].transform;
                t.localPosition += new Vector3(Random.value < 0.2f ? (Random.value < 0.5f ? -pixel : pixel) : 0f, pixel, 0f);
                motes[i].enabled = Random.value > 0.12f;
                if (--life[i] <= 0 || t.localPosition.y > area.yMax) Respawn(i, area.yMin + Random.Range(0f, area.height * 0.3f));
            }
        }
    }
}
