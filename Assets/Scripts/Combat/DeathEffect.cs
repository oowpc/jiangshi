using System.Collections;
using UnityEngine;

namespace Jiangshi.Combat
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DeathEffect : MonoBehaviour
    {
        [SerializeField] private float duration = 0.4f;

        private SpriteRenderer sr;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        public void Play()
        {
            StartCoroutine(DoDeathEffect());
        }

        private IEnumerator DoDeathEffect()
        {
            var startScale = transform.localScale;
            var startColor = sr.color;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                transform.localScale = startScale * (1f - t * 0.5f);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
