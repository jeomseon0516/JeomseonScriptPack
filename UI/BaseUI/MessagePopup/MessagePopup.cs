using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Jeomseon.UI.Components
{
    public sealed class MessagePopup : BaseUI
    {
        [Header("Button")]
        [SerializeField]
        private Button _okButton;

        [Header("Text")]
        [SerializeField]
        private TMP_Text _warningText;
        [SerializeField]
        private TMP_Text _titleText;

        public string WarningText
        {
            get => _warningText.text;
            set => _warningText.text = value;
        }

        public string TitleText
        {
            get => _titleText.text;
            set => _titleText.text = value;
        }

        protected override void EnableUI() { }

        private void Start()
        {
            _okButton.onClick.AddListener(() => UIManager.Instance.CloseUI(this));
        }
    }
}
