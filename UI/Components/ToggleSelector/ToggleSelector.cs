using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Jeomseon.UI.Components
{
    [DisallowMultipleComponent, RequireComponent(typeof(ToggleGroup))]
    public sealed class ToggleSelector : MonoBehaviour
    {
        [field: SerializeField] public UnityEvent<Toggle> OnChangedToggle { get; private set; } = new();

        public ToggleGroup ToggleGroup { get; private set; }
        private Toggle _selectedToggle = null;

        private void Awake()
        {
            ToggleGroup = GetComponent<ToggleGroup>();
        }

        private void Start()
        {
            _selectedToggle = ToggleGroup.GetFirstActiveToggle();

            OnChangedToggle.Invoke(_selectedToggle);
        }

        private void Update()
        {
            if (_selectedToggle != ToggleGroup.GetFirstActiveToggle() && ToggleGroup.GetFirstActiveToggle())
            {
                OnChangedToggle.Invoke(ToggleGroup.GetFirstActiveToggle());
            }

            _selectedToggle = ToggleGroup.GetFirstActiveToggle();
        }

        public Toggle SelectedToggle => ToggleGroup.GetFirstActiveToggle();
    }
}
