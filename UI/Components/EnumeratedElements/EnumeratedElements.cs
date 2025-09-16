using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("com.jeomseon.ui.enumeratedelements.editor")]
#endif

namespace Jeomseon.UI.Components
{
    [RequireComponent(typeof(GridLayoutGroup)), RequireComponent(typeof(ContentSizeFitter)), DisallowMultipleComponent]
    public sealed class EnumeratedElements : MonoBehaviour
    {
        [field: SerializeField] public float WidthToHeightRatio { get; set; }
        [field: SerializeField] public float ElementSizeRatio { get; set; }
        [field: SerializeField] public float ElementSizeToSpacingXRatio { get; set; }
        [field: SerializeField] public float ElementSizeToSpacingYRatio { get; set; }

        [field: SerializeField] public float PaddingLeftRatio { get; set; }
        [field: SerializeField] public float PaddingRightRatio { get; set; }
        [field: SerializeField] public float PaddingTopRatio { get; set; }
        [field: SerializeField] public float PaddingBottomRatio { get; set; }

        private ContentSizeFitter _contentSizeFitter;
        private GridLayoutGroup _gridLayoutGroup;
        private RectTransform _content;

        private void Awake()
        {
            _contentSizeFitter = GetComponent<ContentSizeFitter>();
            _gridLayoutGroup = GetComponent<GridLayoutGroup>();
            _content = GetComponent<RectTransform>();
        }

        private IEnumerator Start()
        {
            InitGridLayoutGroup(_gridLayoutGroup);

            _contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            yield return null;

            _gridLayoutGroup.padding = GetPadding(
                _content.rect.width,
                PaddingLeftRatio,
                PaddingRightRatio,
                PaddingTopRatio,
                PaddingBottomRatio);

            _gridLayoutGroup.cellSize = GetCellSize(
                _content.rect.width,
                ElementSizeRatio,
                WidthToHeightRatio,
                _gridLayoutGroup.constraintCount);

            _gridLayoutGroup.spacing = GetSpacing(
                _gridLayoutGroup.cellSize,
                ElementSizeToSpacingXRatio,
                ElementSizeToSpacingYRatio);
        }

        internal static void InitGridLayoutGroup(GridLayoutGroup gridLayoutGroup)
        {
            gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        }

        internal static RectOffset GetPadding(float contentWidth, float paddingLeftRatio, float paddingRightRatio, float paddingTopRatio, float paddingBottomRatio)
            => new(
                (int)(contentWidth * paddingLeftRatio),
                (int)(contentWidth * paddingRightRatio),
                (int)(contentWidth * paddingTopRatio),
                (int)(contentWidth * paddingBottomRatio));

        internal static Vector2 GetCellSize(float contentWidth, float elementSizeRatio, float widthToHeightRatio, int constraintCount)
        {
            float cellWidth = contentWidth / constraintCount * elementSizeRatio;
            float cellHeight = cellWidth * widthToHeightRatio;

            return new(cellWidth, cellHeight);
        }

        internal static Vector2 GetSpacing(Vector2 cellSize, float elementSizeToSpacingXRatio, float elementSizeToSpacingYRatio)
            => new(cellSize.x * elementSizeToSpacingXRatio, cellSize.y * elementSizeToSpacingYRatio);
    }
}
