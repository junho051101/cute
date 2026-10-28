using System.Collections.Generic;
using Hamster.Art;
using UnityEngine;

namespace Hamster.Village
{
    // Lower on screen draws in front. Only moving sprites carry this; static ones use Order() once.
    // Anchor lets child sprites sort with their owner.
    public class YSort : MonoBehaviour
    {
        public Transform anchor;
        public int offset;
        SpriteRenderer sr;

        public static int Order(float y) => Mathf.RoundToInt(-y * 100f);

        void Awake() => sr = GetComponent<SpriteRenderer>();

        void LateUpdate()
        {
            sr.sortingOrder = Order((anchor != null ? anchor : transform).position.y) + offset;
        }
    }

    public class Bobbing : MonoBehaviour
    {
        public float amplitude = 0.08f;
        public float speed = 1.6f;
        // World sprites step in whole art pixels so they don't shimmer; 0 = smooth (UI).
        public float snap = 1f / PixelArtLibrary.PixelsPerUnit;
        Vector3 basePos;
        float phase;

        void Start()
        {
            basePos = transform.localPosition;
            phase = Random.value * Mathf.PI * 2f;
        }

        void Update()
        {
            float y = Mathf.Sin(Time.time * speed + phase) * amplitude;
            if (snap > 0f) y = Mathf.Round(y / snap) * snap;
            transform.localPosition = basePos + Vector3.up * y;
        }
    }

    // Glowing pastel motes drifting upward around the camera for the dreamy mood.
    public class DreamMotes : MonoBehaviour
    {
        public int count = 26;
        public Camera cam;

        static readonly Color[] Tints =
        {
            new Color(1f, 0.82f, 0.92f), new Color(0.86f, 0.8f, 1f),
            new Color(0.8f, 0.95f, 0.9f), new Color(1f, 0.96f, 0.78f)
        };

        class Mote
        {
            public Transform t;
            public SpriteRenderer sr;
            public float speed, sway, phase, life, maxLife;
            public Color tint;
        }

        readonly List<Mote> motes = new List<Mote>();

        void Start()
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Mote");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PixelArtLibrary.Mote();
                sr.sortingOrder = 32000;
                var m = new Mote { t = go.transform, sr = sr };
                Respawn(m, true);
                motes.Add(m);
            }
        }

        void Respawn(Mote m, bool anywhere)
        {
            var c = cam != null ? cam.transform.position : Vector3.zero;
            float h = cam != null ? cam.orthographicSize : 8f;
            float w = h * (cam != null ? cam.aspect : 0.56f);
            float y = anywhere ? Random.Range(-h, h) : -h - 0.5f;
            m.t.position = new Vector3(c.x + Random.Range(-w, w), c.y + y, 0f);
            m.speed = Random.Range(0.15f, 0.45f);
            m.sway = Random.Range(0.1f, 0.35f);
            m.phase = Random.value * 10f;
            m.maxLife = Random.Range(6f, 12f);
            m.life = anywhere ? Random.Range(0f, m.maxLife) : 0f;
            m.tint = Tints[Random.Range(0, Tints.Length)];
            float s = Random.Range(0.8f, 1.6f);
            m.t.localScale = new Vector3(s, s, 1f);
        }

        void Update()
        {
            foreach (var m in motes)
            {
                m.life += Time.deltaTime;
                var p = m.t.position;
                p.y += m.speed * Time.deltaTime;
                p.x += Mathf.Sin(Time.time * 0.8f + m.phase) * m.sway * Time.deltaTime;
                m.t.position = p;
                float fade = Mathf.Sin(Mathf.Clamp01(m.life / m.maxLife) * Mathf.PI);
                float twinkle = 0.75f + 0.25f * Mathf.Sin(Time.time * 3f + m.phase);
                var col = m.tint;
                col.a = fade * twinkle;
                m.sr.color = col;
                if (m.life >= m.maxLife) Respawn(m, false);
            }
        }
    }

    // Little arcs of water from the fountain spout.
    public class FountainDroplets : MonoBehaviour
    {
        public Vector2 spout = new Vector2(0f, 2.2f);
        public int count = 10;
        public int sortingOrder;

        class Drop
        {
            public Transform t;
            public SpriteRenderer sr;
            public Vector2 vel;
            public float life;
        }

        readonly List<Drop> drops = new List<Drop>();

        void Start()
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Droplet");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PixelArtLibrary.Mote();
                sr.color = new Color(0.85f, 0.95f, 1f);
                sr.sortingOrder = sortingOrder;
                var d = new Drop { t = go.transform, sr = sr };
                Respawn(d);
                d.life = Random.Range(0f, 1.2f);
                drops.Add(d);
            }
        }

        void Respawn(Drop d)
        {
            d.t.localPosition = spout;
            d.vel = new Vector2(Random.Range(-1.1f, 1.1f), Random.Range(1.2f, 2f));
            d.life = 0f;
        }

        void Update()
        {
            foreach (var d in drops)
            {
                d.life += Time.deltaTime;
                d.vel.y -= 5f * Time.deltaTime;
                d.t.localPosition += (Vector3)(d.vel * Time.deltaTime);
                if (d.t.localPosition.y < 0.9f) Respawn(d);
            }
        }
    }
}
