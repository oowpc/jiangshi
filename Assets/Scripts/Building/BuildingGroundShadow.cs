using UnityEngine;

namespace Jiangshi.Building
{
    public sealed class BuildingGroundShadow : MonoBehaviour
    {
        private const string ShadowName = "Ground Shadow";
        private static Sprite shadowSprite;

        private SpriteRenderer shadowRenderer;

        public void Configure(Vector2Int occupiedSize)
        {
            EnsureShadow();

            var width = Mathf.Max(1f, occupiedSize.x) * 1.18f;
            var height = Mathf.Max(1f, occupiedSize.y) * 0.92f;
            shadowRenderer.transform.localScale = new Vector3(width, height, 1f);

            var sourceRenderer = GetComponent<SpriteRenderer>();
            shadowRenderer.sortingOrder = sourceRenderer != null ? sourceRenderer.sortingOrder - 1 : -1;
        }

        private void EnsureShadow()
        {
            if (shadowRenderer != null)
            {
                return;
            }

            var existing = transform.Find(ShadowName);
            var shadow = existing != null ? existing.gameObject : new GameObject(ShadowName);
            shadow.transform.SetParent(transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            shadow.transform.localRotation = Quaternion.identity;

            shadowRenderer = shadow.GetComponent<SpriteRenderer>();
            if (shadowRenderer == null)
            {
                shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            }

            shadowRenderer.sprite = GetShadowSprite();
            shadowRenderer.color = new Color(0f, 0f, 0f, 0.32f);
            shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shadowRenderer.receiveShadows = false;
        }

        private static Sprite GetShadowSprite()
        {
            if (shadowSprite != null)
            {
                return shadowSprite;
            }

            const int width = 128;
            const int height = 64;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Building Ground Shadow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            var radius = new Vector2(width * 0.48f, height * 0.45f);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var normalized = new Vector2((x - center.x) / radius.x, (y - center.y) / radius.y);
                    var distance = normalized.sqrMagnitude;
                    var alpha = distance <= 1f ? Mathf.Pow(1f - distance, 1.25f) : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            shadowSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), width);
            shadowSprite.name = "Building Ground Shadow";
            return shadowSprite;
        }
    }
}
