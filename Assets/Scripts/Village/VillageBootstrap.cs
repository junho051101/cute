using System.Collections.Generic;
using Hamster.Art;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Hamster.Village
{
    // Builds the whole village from Resources/data/village.json, so the scene only needs this one component.
    // In edit mode it builds an unsaved preview (no player, UI or camera) so the Scene view shows the layout.
    [ExecuteAlways]
    public class VillageBootstrap : MonoBehaviour
    {
        const float FountainSpoutHeight = 35f / PixelArtLibrary.PixelsPerUnit;

        VillageData data;
        VillagePlayer player;
        VillageUI ui;
        FloatingJoystick joystick;
        readonly List<(BuildingData data, Vector2 door)> doors = new List<(BuildingData, Vector2)>();
        Transform root;
        Sprite groundSprite;

        void Awake()
        {
            // iOS and Android both default to 30fps; walking feels choppy there.
            if (Application.isPlaying) Application.targetFrameRate = 60;
        }

        void OnEnable()
        {
            if (Application.isPlaying || !gameObject.scene.IsValid()) return;
#if UNITY_EDITOR
            // Clear before a script reload: by OnDisable the preview's Light2Ds are already unregistered
            // and destroying them then trips URP's Light2DManager assertion.
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += ClearPreview;
#endif
            BuildPreview();
        }

        void OnDisable()
        {
            if (Application.isPlaying) return;
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= ClearPreview;
#endif
            ClearPreview();
        }

        [ContextMenu("Rebuild Scene Preview")]
        void BuildPreview()
        {
            ClearPreview();
            var go = new GameObject("VillagePreview (not saved)");
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(transform, false);
            root = go.transform;
            BuildWorld();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags = HideFlags.DontSave;
        }

        void ClearPreview()
        {
            if (root != null && root != transform) DestroyImmediate(root.gameObject);
            root = null;
            // The ground texture is unique per build, so free it instead of leaking one per rebuild.
            if (groundSprite != null)
            {
                DestroyImmediate(groundSprite.texture);
                DestroyImmediate(groundSprite);
            }
            groundSprite = null;
        }

        void Start()
        {
            if (!Application.isPlaying) return;
            root = transform;
            BuildWorld();

            ui = new GameObject("UI").AddComponent<VillageUI>();
            ui.transform.SetParent(root, false);
            ui.Build();
            joystick = ui.gameObject.AddComponent<FloatingJoystick>();
            joystick.Build(ui.Root);

            BuildPlayer();
            var cam = BuildCamera();

            var motes = new GameObject("DreamMotes").AddComponent<DreamMotes>();
            motes.transform.SetParent(root, false);
            motes.cam = cam;
        }

        // Everything static: lights, ground, fountain, buildings, trees.
        void BuildWorld()
        {
            data = VillageData.Load();
            doors.Clear();
            foreach (var b in data.buildings)
                doors.Add((b, b.position.ToVector2() + new Vector2(0f, -0.35f)));

            BuildLighting();
            BuildGround();
            BuildBounds();
            BuildFountain();
            foreach (var b in data.buildings) BuildBuilding(b);
            BuildNature();
        }

        void Update()
        {
            if (player == null) return;
            joystick.Locked = ui.PanelOpen;

            Vector2 p = player.transform.position;
            BuildingData nearest = null;
            float best = data.interactRange * data.interactRange;
            foreach (var (b, door) in doors)
            {
                float d = (p - door).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    nearest = b;
                }
            }
            if (nearest != null) ui.ShowPrompt(nearest);
            else ui.HidePrompt();
        }

        void BuildLighting()
        {
            var global = new GameObject("GlobalLight").AddComponent<Light2D>();
            global.transform.SetParent(root, false);
            global.lightType = Light2D.LightType.Global;
            global.color = PixelArtLibrary.Hex("FFF2FB");
            global.intensity = 0.92f;
        }

        void AddGlow(Transform parent, Vector2 localPos, string hex, float radius, float intensity)
        {
            var light = new GameObject("Glow").AddComponent<Light2D>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = localPos;
            light.lightType = Light2D.LightType.Point;
            light.color = PixelArtLibrary.Hex(hex);
            light.intensity = intensity;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = 0f;
            light.falloffIntensity = 0.75f;
            light.shadowsEnabled = false;
        }

        void BuildGround()
        {
            var layout = new PixelArtLibrary.GroundLayout
            {
                mapWidth = data.mapWidth,
                mapHeight = data.mapHeight,
                plaza = data.plaza.ToRect(),
                seed = data.seed
            };
            foreach (var p in data.paths ?? new PathData[0])
            {
                var points = new Vector2[p.points.Length];
                for (int i = 0; i < points.Length; i++) points[i] = p.points[i].ToVector2();
                layout.paths.Add((points, p.width));
            }
            foreach (var a in data.fields ?? new Area[0]) layout.fields.Add(a.ToRect());
            foreach (var a in data.flowerBeds ?? new Area[0]) layout.flowerBeds.Add(a.ToRect());

            var go = new GameObject("Ground");
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = groundSprite = PixelArtLibrary.Ground(layout);
            sr.sortingOrder = -32000;
        }

        void BuildBounds()
        {
            float hw = data.mapWidth / 2f, hh = data.mapHeight / 2f;
            var bounds = new GameObject("Bounds");
            bounds.transform.SetParent(root, false);
            void Wall(Vector2 center, Vector2 size)
            {
                var box = bounds.AddComponent<BoxCollider2D>();
                box.offset = center;
                box.size = size;
            }
            Wall(new Vector2(0f, hh + 0.5f), new Vector2(data.mapWidth + 2f, 1f));
            Wall(new Vector2(0f, -hh - 0.5f), new Vector2(data.mapWidth + 2f, 1f));
            Wall(new Vector2(-hw - 0.5f, 0f), new Vector2(1f, data.mapHeight + 2f));
            Wall(new Vector2(hw + 0.5f, 0f), new Vector2(1f, data.mapHeight + 2f));
        }

        void BuildFountain()
        {
            var pos = data.fountain.ToVector2();
            var go = MakeSprite("Fountain", PixelArtLibrary.Fountain(), pos, root);
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 1.25f;
            col.offset = new Vector2(0f, 0.65f);
            AddGlow(go.transform, new Vector2(0f, 1f), "D8EEFF", 4.5f, 0.55f);

            var drops = go.AddComponent<FountainDroplets>();
            drops.spout = new Vector2(0f, FountainSpoutHeight);
            drops.sortingOrder = YSort.Order(pos.y) + 5;
        }

        void BuildBuilding(BuildingData b)
        {
            var pos = b.position.ToVector2();
            var sprite = PixelArtLibrary.House(b.roofColor, b.style);
            var go = MakeSprite(b.id, sprite, pos, root);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(b.style == "tower" ? 1.35f : 2.25f, 1.2f);
            box.offset = new Vector2(0f, 0.85f);
            AddGlow(go.transform, new Vector2(0f, 0.6f), "FFE6A8", 1.8f, 0.5f);

            float top = sprite.rect.height / PixelArtLibrary.PixelsPerUnit;
            var sign = MakeSprite("Sign", PixelArtLibrary.Sign(b.icon), pos + new Vector2(0f, top - 0.1f), go.transform);
            sign.GetComponent<SpriteRenderer>().sortingOrder = YSort.Order(pos.y) + 1;
            sign.AddComponent<Bobbing>();
        }

        // Trees and bushes come straight from village.json; nothing is scattered at random.
        void BuildNature()
        {
            var nature = new GameObject("Nature").transform;
            nature.SetParent(root, false);

            foreach (var t in data.trees ?? new Prop[0])
            {
                var go = MakeSprite("Tree", PixelArtLibrary.Tree(t.variant), t.ToVector2(), nature);
                var col = go.AddComponent<CircleCollider2D>();
                col.radius = 0.35f;
                col.offset = new Vector2(0f, 0.3f);
            }
            foreach (var b in data.bushes ?? new Prop[0])
                MakeSprite("Bush", PixelArtLibrary.Bush(), b.ToVector2(), nature);
        }

        void BuildPlayer()
        {
            var start = data.playerStart.ToVector2();
            var go = new GameObject("Player");
            go.transform.SetParent(root, false);
            go.transform.position = start;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
            col.offset = new Vector2(0f, 0.3f);
            go.AddComponent<Rigidbody2D>();

            // Sprites hang under a grid-snapped child; the physics body itself moves freely.
            var pixelRoot = new GameObject("PixelSnap").transform;
            pixelRoot.SetParent(go.transform, false);

            var shadow = MakeSprite("Shadow", PixelArtLibrary.Shadow(16), start, pixelRoot);
            shadow.transform.localPosition = Vector3.zero;
            var shadowSort = shadow.AddComponent<YSort>();
            shadowSort.anchor = go.transform;
            shadowSort.offset = -1;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(pixelRoot, false);
            var body = MakeSprite("Body", PixelArtLibrary.HamsterFrame(PixelArtLibrary.Facing.Front, 0), start, visual);
            body.transform.localPosition = Vector3.zero;
            body.AddComponent<YSort>().anchor = go.transform;

            player = go.AddComponent<VillagePlayer>();
            player.joystick = joystick;
            player.moveSpeed = data.moveSpeed;
            player.pixelRoot = pixelRoot;
            player.visual = visual;
            player.body = body.GetComponent<SpriteRenderer>();
        }

        Camera BuildCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            // Matches the grass so wide screens that see past the map edge show no colored border.
            cam.backgroundColor = PixelArtLibrary.Hex("B5E0B0");
            cam.transform.position = new Vector3(0f, 0f, -10f);

            var fixedCam = GetOrAdd<MapCamera>(cam.gameObject);
            fixedCam.mapSize = new Vector2(data.mapWidth, data.mapHeight);
            fixedCam.mapCenter = Vector2.zero;
            fixedCam.Fit(true);
            return cam;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        // Static sprites get their sort order once here; only moving ones add a YSort.
        // Positions land on the art pixel grid so every sprite rounds the same way as the camera moves.
        static GameObject MakeSprite(string name, Sprite sprite, Vector2 worldPos, Transform parent)
        {
            const float ppu = PixelArtLibrary.PixelsPerUnit;
            worldPos = new Vector2(Mathf.Round(worldPos.x * ppu) / ppu, Mathf.Round(worldPos.y * ppu) / ppu);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = YSort.Order(worldPos.y);
            return go;
        }
    }
}
