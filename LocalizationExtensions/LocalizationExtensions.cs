using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Jeomseon.LocalizationExtensions
{
    public static class LocalizationExtensions
    {
        public static bool TryGetLocalizedString(this LocalizedString localizedString, out string localizedText)
        {
            if (localizedString.IsEmpty)
            {
                localizedText = "";
                return false;
            }

            localizedText = localizedString.GetLocalizedString();
            return true;
        }
        
        public static string TryGetLocalizedString(this StringTable table, string entryName)
        {
            StringTableEntry entry = table.GetEntry(entryName);

            Comment comment = entry.GetMetadata<Comment>();
            if (comment is not null)
            {
                Debug.Log($"Fount metadata comment for {entryName} - {comment.CommentText}");
            }

            return entry.GetLocalizedString();
        }

        public static string GetLocalizedStringByLocale(this LocalizedString localizedString, string localeCode)
        {
            if (localizedString.IsEmpty) return string.Empty;

            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                LocalizationSettings.InitializationOperation.WaitForCompletion();
            }

            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
            return localizedString.GetLocalizedString(locale);
        }

        public static string GetEntryKeyName(this LocalizedString localizedString)
        {
            if (!string.IsNullOrEmpty(localizedString.TableReference))
            {
                LocalizedDatabase<StringTable, StringTableEntry>.TableEntryResult t = 
                    LocalizationSettings.StringDatabase.GetTableEntry(
                        localizedString.TableReference, 
                        localizedString.TableEntryReference);
                return t.Entry?.Key ?? "";
            }

            return "";
        }
    }
}
