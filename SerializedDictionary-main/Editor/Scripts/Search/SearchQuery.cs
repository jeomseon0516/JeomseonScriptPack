using System.Collections.Generic;
using UnityEditor;

namespace AYellowpaper.SerializedCollections.Editor.Search
{
    public class SearchQuery
    {
        public string SearchString
        {
            get => _text;
            set
            {
                if (_text == value)
                    return;

                _text = value;
                foreach (Matcher matcher in _matchers)
                    matcher.Prepare(_text);
            }
        }

        private IEnumerable<Matcher> _matchers;
        private string _text;

        public SearchQuery(IEnumerable<Matcher> matchers)
        {
            _matchers = matchers;
        }

        public List<PropertySearchResult> ApplyToProperty(SerializedProperty property)
        {
            TryGetMatchingProperties(property.Copy(), out List<PropertySearchResult> properties);
            return properties;
        }

        public IEnumerable<SearchResultEntry> ApplyToArrayProperty(SerializedProperty property)
        {
            int arrayCount = property.arraySize;
            for (int i = 0; i < arrayCount; i++)
            {
                SerializedProperty prop = property.GetArrayElementAtIndex(i);
                if (TryGetMatchingProperties(prop.Copy(), out List<PropertySearchResult> properties))
                    yield return new SearchResultEntry(i, prop, properties);
            }
        }

        private bool TryGetMatchingProperties(SerializedProperty property, out List<PropertySearchResult> matchingProperties)
        {
            matchingProperties = null;
            foreach (SerializedProperty child in SCEditorUtility.GetChildren(property, true))
            {
                foreach (Matcher matcher in _matchers)
                {
                    if (matcher.IsMatch(child))
                    {
                        matchingProperties ??= new();
                        matchingProperties.Add(new PropertySearchResult(child.Copy()));
                    }
                }
            }

            return matchingProperties != null;
        }
    }
}