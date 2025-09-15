#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using Unity.EditorCoroutines.Editor;

namespace Jeomseon.LocalizationExtensions.Editor
{
    public static class LocalizedStringExtensionsForEditor
    {
        // Locale 캐시
        private static readonly Dictionary<string, Locale> _localeCache = new();

        public static EditorCoroutine MonitorSpecificLocaleEntry(this LocalizedString localizedString, string localeCode, System.Action<string> onChanged)
        {
            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                LocalizationSettings.InitializationOperation.WaitForCompletion();
            }
            
            // Locale 캐시에서 가져오기
            if (!_localeCache.TryGetValue(localeCode, out Locale targetLocale))
            {
                targetLocale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
                if (targetLocale != null)
                {
                    _localeCache.Add(localeCode, targetLocale); // Locale 캐시
                }
                else
                {
                    Debug.LogError($"'{localeCode}'에 해당하는 Locale을 찾을 수 없습니다.");
                    return null;
                }
            }
            
            return EditorCoroutineUtility.StartCoroutineOwnerless(iEMonitorEntryCoroutine(localizedString, targetLocale, onChanged));
        }

        private static IEnumerator iEMonitorEntryCoroutine(LocalizedString localizedString, Locale locale, System.Action<string> onChanged)
        {
            findTable:
            yield return new WaitUntil(() => localizedString.TableReference.ReferenceType != TableReference.Type.Empty);
            AsyncOperationHandle<StringTable> tableOperation = LocalizationSettings.StringDatabase.GetTableAsync(localizedString.TableReference, locale);
            yield return tableOperation;

            StringTable stringTable = tableOperation.Result;
            if (stringTable == null)
            {
                Debug.LogError($"Locale '{locale.LocaleName}'의 StringTable을 찾을 수 없습니다.");
                yield break;
            }

            string previousValue = getEntryValue(stringTable, localizedString.TableEntryReference.KeyId);
            onChanged?.Invoke(previousValue);
            // 주기적으로 값 모니터링
            while (Application.isPlaying == false)
            {
                if (localizedString.TableReference.ReferenceType == TableReference.Type.Empty)
                {
                    onChanged?.Invoke(string.Empty);
                    goto findTable;
                }
                
                string currentValue = getEntryValue(stringTable, localizedString.TableEntryReference.KeyId);
                if (currentValue != previousValue)
                {
                    previousValue = currentValue;
                    onChanged?.Invoke(currentValue);
                }

                yield return new EditorWaitForSeconds(1f); // 주기적으로 확인
            }
        }

        private static string getEntryValue(StringTable table, long keyId)
        {
            return table.TryGetValue(keyId, out StringTableEntry entry) ? entry.GetLocalizedString() : string.Empty;
        }
    }
}
#endif