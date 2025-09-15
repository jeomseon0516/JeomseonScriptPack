#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using static UnityEngine.ScriptableObject;
using static UnityEditor.AssetDatabase;

namespace Jeomseon.ScriptableObjects.Editor
{
    public static class ScriptableObjectHandler
    {
        public static bool CheckDuplicatedName(string dataPath, string assetName)
            => File.Exists($"{dataPath}/{assetName}.asset");

        public static T CreateScriptableObject<T>(string dataPath, string assetName) where T : UnityEngine.ScriptableObject
        {
            T asset = CreateInstance<T>();
            string fullPath = $"{dataPath}/{assetName}.asset";
            string directory = Path.GetDirectoryName(fullPath)!;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            CreateAsset(asset, fullPath);
            SaveData(asset);

            return asset;
        }

        public static UnityEngine.ScriptableObject CreateScriptableObject(string dataPath, string assetName, Type type)
        {
            if (!type.IsSubclassOf(typeof(UnityEngine.ScriptableObject))) return null;

            UnityEngine.ScriptableObject asset = CreateInstance(type);
            string fullPath = $"{dataPath}/{assetName}.asset";
            string directory = Path.GetDirectoryName(fullPath)!;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            CreateAsset(asset, fullPath);
            SaveData(asset);
            
            return asset;
        }

        public static UnityEngine.ScriptableObject[] LoadAllScriptableObjects(Type type)
        {
            return FindAssets($"t:{type.Name}")
                .Select(guid => LoadAssetAtPath<ScriptableObject>(GUIDToAssetPath(guid)))
                .Where(so => so)
                .ToArray();
        }

        public static T[] LoadAllScriptableObjects<T>() where T : UnityEngine.ScriptableObject 
            => FindAssets($"t:{typeof(T).Name}")
                .Select(guid => LoadAssetAtPath<T>(GUIDToAssetPath(guid)))
                .ToArray();

        public static void RenameScriptableObject(UnityEngine.ScriptableObject asset, string newName)
        {
            RenameAsset(GetAssetPath(asset), newName);
            SaveData(asset);
        }

        public static void SaveData(UnityEngine.ScriptableObject asset)
        {
            EditorUtility.SetDirty(asset);
            SaveAssets();
            Refresh();
        }
    }
}
#endif