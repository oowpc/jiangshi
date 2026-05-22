using UnityEngine;
using UnityEngine.UI;

namespace Jiangshi.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoundedBox : MaskableGraphic
    {
        [SerializeField] private float cornerRadius = 18f;
        [SerializeField] private int cornerSegments = 8;

        public float CornerRadius
        {
            get => cornerRadius;
            set
            {
                cornerRadius = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }

        public int CornerSegments
        {
            get => cornerSegments;
            set
            {
                cornerSegments = Mathf.Max(1, value);
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            var rect = GetPixelAdjustedRect();
            var radius = Mathf.Min(cornerRadius, rect.width * 0.5f, rect.height * 0.5f);
            if (radius <= 0.01f)
            {
                AddRectangle(vertexHelper, rect);
                return;
            }

            var center = rect.center;
            AddVertex(vertexHelper, center);

            var segments = Mathf.Max(1, cornerSegments);
            AddCorner(vertexHelper, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f, segments);
            AddCorner(vertexHelper, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f, segments);
            AddCorner(vertexHelper, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f, segments);
            AddCorner(vertexHelper, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f, segments);

            var vertexCount = vertexHelper.currentVertCount;
            for (var i = 1; i < vertexCount; i++)
            {
                var next = i == vertexCount - 1 ? 1 : i + 1;
                vertexHelper.AddTriangle(0, i, next);
            }
        }

        private void AddRectangle(VertexHelper vertexHelper, Rect rect)
        {
            AddVertex(vertexHelper, new Vector2(rect.xMin, rect.yMin));
            AddVertex(vertexHelper, new Vector2(rect.xMin, rect.yMax));
            AddVertex(vertexHelper, new Vector2(rect.xMax, rect.yMax));
            AddVertex(vertexHelper, new Vector2(rect.xMax, rect.yMin));

            vertexHelper.AddTriangle(0, 1, 2);
            vertexHelper.AddTriangle(2, 3, 0);
        }

        private void AddCorner(VertexHelper vertexHelper, Vector2 center, float radius, float startAngle, float endAngle, int segments)
        {
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
                var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                AddVertex(vertexHelper, point);
            }
        }

        private void AddVertex(VertexHelper vertexHelper, Vector2 position)
        {
            var vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = position;
            vertexHelper.AddVert(vertex);
        }
    }
}
