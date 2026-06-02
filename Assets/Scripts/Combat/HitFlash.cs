using System.Collections;
using UnityEngine;

namespace Jiangshi.Combat
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class HitFlash : MonoBehaviour
    {
        [SerializeField] private Color flashColor = Color.red;
        [SerializeField] private float duration = 0.1f;

        private SpriteRenderer sr;
        private Color originalColor;
        private Coroutine flashRoutine;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            originalColor = sr.color;
        }

        public void Flash()
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(DoFlash());
        }

        private IEnumerator DoFlash()
        {
            sr.color = flashColor;
            yield return new WaitForSeconds(duration);
            sr.color = originalColor;
            flashRoutine = null;
        }
    }
}
