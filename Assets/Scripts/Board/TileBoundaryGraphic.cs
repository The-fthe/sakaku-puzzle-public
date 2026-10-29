using UnityEngine;
using UnityEngine.UI;

namespace SakakuPuzzle
{
    [DisallowMultipleComponent]
    public sealed class TileBoundaryGraphic : MaskableGraphic
    {
        const int GRID_SIZE = 5;

        [SerializeField, Min(0.5f)] float _strokeWidth = 2f;

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var temporaryRect = GetPixelAdjustedRect();
            var temporaryHalfStroke = _strokeWidth * 0.5f;
            for (var temporaryIndex = 1;
                 temporaryIndex < GRID_SIZE;
                 temporaryIndex++)
            {
                var temporaryX =
                    Mathf.Lerp(
                        temporaryRect.xMin,
                        temporaryRect.xMax,
                        temporaryIndex / (float)GRID_SIZE);
                AddQuad(
                    vertexHelper,
                    new Rect(
                        temporaryX - temporaryHalfStroke,
                        temporaryRect.yMin,
                        _strokeWidth,
                        temporaryRect.height));

                var temporaryY =
                    Mathf.Lerp(
                        temporaryRect.yMin,
                        temporaryRect.yMax,
                        temporaryIndex / (float)GRID_SIZE);
                for (var temporarySegmentIndex = 0;
                     temporarySegmentIndex < GRID_SIZE;
                     temporarySegmentIndex++)
                {
                    var temporaryStartX =
                        Mathf.Lerp(
                            temporaryRect.xMin,
                            temporaryRect.xMax,
                            temporarySegmentIndex /
                            (float)GRID_SIZE);
                    var temporaryEndX =
                        Mathf.Lerp(
                            temporaryRect.xMin,
                            temporaryRect.xMax,
                            (temporarySegmentIndex + 1) /
                            (float)GRID_SIZE);
                    if (temporarySegmentIndex > 0)
                    {
                        temporaryStartX += temporaryHalfStroke;
                    }

                    if (temporarySegmentIndex < GRID_SIZE - 1)
                    {
                        temporaryEndX -= temporaryHalfStroke;
                    }

                    AddQuad(
                        vertexHelper,
                        new Rect(
                            temporaryStartX,
                            temporaryY - temporaryHalfStroke,
                            temporaryEndX - temporaryStartX,
                            _strokeWidth));
                }
            }
        }

        void AddQuad(VertexHelper vertexHelper, Rect rect)
        {
            var temporaryStartIndex = vertexHelper.currentVertCount;
            var temporaryColor = (Color32)color;
            vertexHelper.AddVert(
                new Vector3(rect.xMin, rect.yMin),
                temporaryColor,
                Vector2.zero);
            vertexHelper.AddVert(
                new Vector3(rect.xMin, rect.yMax),
                temporaryColor,
                Vector2.up);
            vertexHelper.AddVert(
                new Vector3(rect.xMax, rect.yMax),
                temporaryColor,
                Vector2.one);
            vertexHelper.AddVert(
                new Vector3(rect.xMax, rect.yMin),
                temporaryColor,
                Vector2.right);
            vertexHelper.AddTriangle(
                temporaryStartIndex,
                temporaryStartIndex + 1,
                temporaryStartIndex + 2);
            vertexHelper.AddTriangle(
                temporaryStartIndex + 2,
                temporaryStartIndex + 3,
                temporaryStartIndex);
        }
    }
}
