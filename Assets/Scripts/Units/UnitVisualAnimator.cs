using UnityEngine;

namespace Jiangshi.Units
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class UnitVisualAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Sprite[] reloadFrames;
        [SerializeField] private Sprite[] deathFrames;
        [SerializeField] private float idleFps = 6f;
        [SerializeField] private float walkFps = 8f;
        [SerializeField] private float attackFps = 10f;
        [SerializeField] private float reloadFps = 8f;
        [SerializeField] private float deathFps = 8f;

        private SpriteRenderer spriteRenderer;
        private Sprite[] currentFrames;
        private float currentFps;
        private float timer;
        private int frameIndex;
        private bool oneShot;
        private bool locked;

        public bool IsPlayingOneShot => oneShot && !locked;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            PlayLoop(idleFrames, idleFps);
        }

        private void Update()
        {
            if (currentFrames == null || currentFrames.Length <= 1 || currentFps <= 0f)
            {
                return;
            }

            timer += Time.deltaTime;
            var frameDuration = 1f / currentFps;
            while (timer >= frameDuration)
            {
                timer -= frameDuration;
                AdvanceFrame();
            }
        }

        public void PlayIdle()
        {
            if (!CanPlayLoop()) return;
            PlayLoop(idleFrames, idleFps);
        }

        public void PlayWalk()
        {
            if (!CanPlayLoop()) return;
            PlayLoop(walkFrames, walkFps);
        }

        public void PlayAttack()
        {
            PlayOneShot(attackFrames, attackFps);
        }

        public void PlayReload()
        {
            if (IsPlayingOneShot) return;
            PlayLoop(reloadFrames, reloadFps);
        }

        public float PlayDeath()
        {
            locked = true;
            PlayLoop(deathFrames, deathFps);
            return currentFrames != null && currentFrames.Length > 0 && currentFps > 0f
                ? currentFrames.Length / currentFps
                : 0f;
        }

        public void SetFacing(Vector3 direction)
        {
            if (spriteRenderer == null || Mathf.Abs(direction.x) < 0.01f)
            {
                return;
            }

            spriteRenderer.flipX = direction.x < 0f;
        }

        private bool CanPlayLoop() => !locked && !IsPlayingOneShot;

        private void PlayLoop(Sprite[] frames, float fps)
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            if (currentFrames == frames && !oneShot)
            {
                return;
            }

            currentFrames = frames;
            currentFps = fps;
            oneShot = false;
            timer = 0f;
            frameIndex = 0;
            spriteRenderer.sprite = currentFrames[frameIndex];
        }

        private void PlayOneShot(Sprite[] frames, float fps)
        {
            if (locked || frames == null || frames.Length == 0)
            {
                return;
            }

            currentFrames = frames;
            currentFps = fps;
            oneShot = true;
            timer = 0f;
            frameIndex = 0;
            spriteRenderer.sprite = currentFrames[frameIndex];
        }

        private void AdvanceFrame()
        {
            frameIndex++;
            if (frameIndex >= currentFrames.Length)
            {
                if (oneShot)
                {
                    oneShot = false;
                    PlayLoop(idleFrames, idleFps);
                    return;
                }

                frameIndex = locked ? currentFrames.Length - 1 : 0;
            }

            spriteRenderer.sprite = currentFrames[frameIndex];
        }
    }
}
