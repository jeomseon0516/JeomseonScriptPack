using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jeomseon.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(Canvas)), RequireComponent(typeof(GraphicRaycaster))]
    public abstract class BaseUI : MonoBehaviour, ISerializationCallbackReceiver
    {
        public bool ActiveSelf => Canvas.enabled;
        public bool HideSelf => GraphicRaycaster.enabled;

        [field: Header("Canvas Options")]
        [field: SerializeField] public GraphicRaycaster GraphicRaycaster { get; private set; }
        [field: SerializeField] public Canvas Canvas { get; private set; }

        public void OnBeforeSerialize()
        {
            if (this == null) return;

            if (!GraphicRaycaster)
            {
                GraphicRaycaster = GetComponent<GraphicRaycaster>();
            }

            if (!Canvas)
            {
                Canvas = GetComponent<Canvas>();
            }
        }

        public void OnAfterDeserialize() { }

        private void OnEnable()
        {
            transform.SetAsLastSibling();
            EnableUI();
        }

        public void CloseUI()
        {
            UIManager.Instance.CloseUI(this);
        }

        public void SetActive(bool isActive)
        {
            enabled = isActive;
            Canvas.enabled = isActive;
            GraphicRaycaster.enabled = isActive;
        }

        protected abstract void EnableUI();
    }
}