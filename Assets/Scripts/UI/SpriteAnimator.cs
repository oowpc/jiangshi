using UnityEngine;

namespace Jiangshi.UI
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float fps = 8f;

        private SpriteRenderer sr;
        private float timer;
        private int index;

        public void SetFrames(Sprite[] sprites) => frames = sprites;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (frames == null || frames.Length <= 1) return;

            timer += Time.deltaTime;
            if (timer >= 1f / fps)
            {
                timer -= 1f / fps;
                index = (index + 1) % frames.Length;
                sr.sprite = frames[index];
            }
        }
    }
}
