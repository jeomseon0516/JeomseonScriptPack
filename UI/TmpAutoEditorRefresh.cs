using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Jeomseon.Attribute;

[ExecuteAlways]
public class TmpAutoEditorRefresh : MonoBehaviour
{
    [SerializeField, InitializeRequireComponent] private TextMeshProUGUI _targetText;
    [SerializeField, InitializeRequireComponent] private RectTransform _contentToRefresh;

    private string _lastValue;

    void Update()
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

    void Refresh()
    {
        _targetText.ForceMeshUpdate();

        LayoutRebuilder.ForceRebuildLayoutImmediate(_targetText.rectTransform);

        if (_contentToRefresh != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentToRefresh);

        Canvas.ForceUpdateCanvases();
    }
}
