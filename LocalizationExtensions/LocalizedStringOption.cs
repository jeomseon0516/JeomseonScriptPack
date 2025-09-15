using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Components;
using Jeomseon.Extensions;

namespace Jeomseon.LocalizationExtensions
{
    /// <summary>
    /// .. 현재 사용하지 않습니다
    /// </summary>
    [System.Serializable]
    public sealed class LocalizedStringOption
    {
        public string EntryKeyName => _localizedString.GetEntryKeyName();

        [SerializeField]
        private LocalizedString _localizedString;

        public bool TrySetOption(MonoBehaviour monoBehaviour, UnityAction<string> onChangedText, out LocalizeStringEvent localizeStringEvent)
        {
            if (string.IsNullOrEmpty(EntryKeyName) || !monoBehaviour) // .. 모노비하이비어가 없다면..
            {
                localizeStringEvent = null;
                return false;
            }

            if (!monoBehaviour.TryGetComponent(out localizeStringEvent)) // .. 
            {
                localizeStringEvent = getLocalizeStringEvent(monoBehaviour.gameObject, _localizedString);
            }
            else
            {
                if (localizeStringEvent.StringReference != _localizedString)
                {
                    localizeStringEvent = getLocalizeStringEvent(monoBehaviour.gameObject, _localizedString);
                }
            }

            localizeStringEvent.OnUpdateString.AddListener(onChangedText);
            localizeStringEvent.OnUpdateString.SetPersistentListenerState(UnityEventCallState.EditorAndRuntime);
            localizeStringEvent.RefreshString();

            return true;

            static LocalizeStringEvent getLocalizeStringEvent(GameObject go, LocalizedString localizedString)
            {
                LocalizeStringEvent localizeStringEvent = go.AddComponent<LocalizeStringEvent>();
                localizeStringEvent.StringReference = localizedString;

                return localizeStringEvent;
            }
        }

        public LocalizedStringOption(TableReference tableReference, TableEntryReference entryReference)
        {
            _localizedString = new(tableReference, entryReference);
        }
    }
}