using UnityEngine;
using Jiangshi.Combat;

namespace Jiangshi.UI
{
    [RequireComponent(typeof(Damageable))]
    public sealed class UnitHealthBar : MonoBehaviour
    {
        private Damageable damageable;
        private bool hovered;
        private float showUntil;
        private static readonly Vector2 barSize = new(50f, 6f);

        private void Awake()
        {
            damageable = GetComponent<Damageable>();
        }

        private void OnMouseEnter() => hovered = true;
        private void OnMouseExit() => hovered = false;
        private void OnMouseDown() => showUntil = Time.unscaledTime + 3f;

        private void OnGUI()
        {
            if (damageable == null || damageable.IsDead) return;
            if (!hovered && Time.unscaledTime > showUntil) return;

            var cam = Camera.main;
            if (cam == null) return;

            var worldPos = transform.position + Vector3.up * 1.2f;
            var screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0) return;

            var rect = new Rect(screenPos.x - barSize.x / 2f, Screen.height - screenPos.y - barSize.y, barSize.x, barSize.y);
            var ratio = (float)damageable.CurrentHealth / damageable.MaxHealth;

            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, Color.black, 0, 0);
            var fillRect = new Rect(rect.x + 1, rect.y + 1, (rect.width - 2) * ratio, rect.height - 2);
            var color = ratio > 0.5f ? Color.green : ratio > 0.25f ? Color.yellow : Color.red;
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, color, 0, 0);
        }
    }
}
