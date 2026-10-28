using Hamster.Art;
using UnityEngine;

namespace Hamster.Village
{
    // Village-only movement with a 4-frame walk cycle facing side, front or back.
    [RequireComponent(typeof(Rigidbody2D))]
    public class VillagePlayer : MonoBehaviour
    {
        public FloatingJoystick joystick;
        public float moveSpeed = 4f;
        // Holds every sprite of the player; snapped to the art pixel grid each frame.
        public Transform pixelRoot;
        public Transform visual;
        public SpriteRenderer body;
        public float stepsPerSecond = 9f;

        Rigidbody2D rb;
        Vector2 move;
        float walkTime;
        PixelArtLibrary.Facing facing = PixelArtLibrary.Facing.Front;
        bool faceLeft;
        Sprite shown;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        void Update()
        {
            move = joystick != null ? joystick.Value : Vector2.zero;
            Animate();
        }

        void FixedUpdate()
        {
            rb.linearVelocity = move * moveSpeed;
        }

        // The camera renders at art resolution, so a sprite between art pixels rounds differently
        // from frame to frame and wobbles by a whole pixel. Physics keeps the exact position; only the
        // sprites are snapped, after interpolation has moved the body this frame.
        void LateUpdate()
        {
            if (pixelRoot == null) return;
            const float ppu = PixelArtLibrary.PixelsPerUnit;
            Vector2 p = transform.position;
            var snapped = new Vector2(Mathf.Round(p.x * ppu) / ppu, Mathf.Round(p.y * ppu) / ppu);
            pixelRoot.localPosition = snapped - p;
        }

        // Pose changes come only from whole-pixel frames; scaling pixel art makes pixels pop in and out.
        void Animate()
        {
            int frame = 0;
            float m = move.magnitude;
            if (m > 0.1f)
            {
                UpdateFacing();
                walkTime = Mathf.Repeat(walkTime + Time.deltaTime * stepsPerSecond * Mathf.Lerp(0.6f, 1f, m), PixelArtLibrary.WalkFrames);
                frame = (int)walkTime;
            }
            else
            {
                walkTime = 0f;
            }

            var sprite = PixelArtLibrary.HamsterFrame(facing, frame);
            if (sprite != shown)
            {
                body.sprite = sprite;
                shown = sprite;
            }
            body.flipX = facing == PixelArtLibrary.Facing.Side && faceLeft;
        }

        // Hysteresis keeps diagonals from flickering between side and front/back.
        void UpdateFacing()
        {
            float ax = Mathf.Abs(move.x), ay = Mathf.Abs(move.y);
            if (ax > 0.1f) faceLeft = move.x < 0f;
            bool side = facing == PixelArtLibrary.Facing.Side;
            if (side && ay > ax * 1.3f) side = false;
            else if (!side && ax > ay * 1.3f) side = true;
            facing = side ? PixelArtLibrary.Facing.Side
                : move.y > 0f ? PixelArtLibrary.Facing.Back : PixelArtLibrary.Facing.Front;
        }
    }
}
