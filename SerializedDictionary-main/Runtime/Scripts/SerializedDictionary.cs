using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AYellowpaper.SerializedCollections
{
    [System.Serializable]
    public class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        internal IKeyable LookupTable
        {
            get
            {
                _lookupTable ??= new(this);
                return _lookupTable;
            }
        }

        private DictionaryLookupTable<TKey, TValue> _lookupTable;
        private bool _isCallSave = false;
#endif

        [SerializeField]
        internal List<SerializedKeyValuePair<TKey, TValue>> _serializedList = new();

        public void OnAfterDeserialize()
        {
            serialize();
        }

        private void serialize()
        {
            base.Clear();

            foreach (SerializedKeyValuePair<TKey, TValue> kvp in _serializedList.Where(kvp => !ContainsKey(kvp.Key)))
            {
                base.Add(kvp.Key, kvp.Value);
            }

#if UNITY_EDITOR
            LookupTable.RecalculateOccurences();
#else
            _serializedList.Clear();
#endif
        }

        public new void Clear()
        {
            #if UNITY_EDITOR
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (!Application.isPlaying)
            {
                _serializedList.Clear();
                callSave();
            }
            #endif

            base.Clear();
        }

        public new void Add(TKey key, TValue value)
        {
            SerializedKeyValuePair<TKey, TValue> kvp = new(key, value);
            #if UNITY_EDITOR
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (!Application.isPlaying)
            {
                _serializedList.Add(kvp);
                callSave();
            }
            #endif

            base.Add(key, value);
        }

        public new bool Remove(TKey key)
        {
            #if UNITY_EDITOR
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (!Application.isPlaying && 0 < _serializedList.RemoveAll(kvp => kvp.Key.Equals(key)))
            {
                callSave();
                return true;
            }
            #endif

            return base.Remove(key);
        }

        #if UNITY_EDITOR
        private void callSave()
        {
            if (_isCallSave) return;
            EditorApplication.delayCall += () =>
            {
                serialize();
                _isCallSave = false;
            };

            _isCallSave = true;
        }
        #endif

        public new TValue this[TKey key]
        {
            get => base[key];
            set
            {
                #if UNITY_EDITOR
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (!Application.isPlaying)
                {
                    int index = _serializedList.FindIndex(kvp => kvp.Key.Equals(key));
                    if (index > -1)
                    {
                        _serializedList[index] = new(key, value);
                        callSave();
                    }
                }
                #endif

                base[key] = value;
            }
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (UnityEditor.BuildPipeline.isBuildingPlayer)
                LookupTable.RemoveDuplicates();
#endif
        }
    }
}