using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Jeomseon.Attribute;

namespace Jeomseon.UI
{
    [ExecuteAlways]
    public class TmpAutoEditorRefresh : MonoBehaviour
    {
        [SerializeField, InitializeRequireComponent] private TextMeshProUGUI _targetText;
        [SerializeField, InitializeRequireComponent] private RectTransform _contentToRefresh;

        private string _lastValue;

        private void Update()
        {
            if (!Application.isPlaying)
            {
                if (_targetText == null) return;

                if (_lastValue != _targetText.text)
                {
                    _lastValue = _targetText.text;
                    Refresh();
                }
            }
        }

        private void Refresh()
        {
            _targetText.ForceMeshUpdate();

            LayoutRebuilder.ForceRebuildLayoutImmediate(_targetText.rectTransform);

            if (_contentToRefresh != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentToRefresh);

            Canvas.ForceUpdateCanvases();
        }
    }
}
