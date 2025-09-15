using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Singleton
{
    public abstract class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObjectSingleton<T>
    {
        public static T Instance => EnsureInstance();
        public static bool HasInstance => _instance != null;

        public static bool ScriptableObjectFileExists
            => HasInstance || File.Exists(getFilePathWithExtension(true));

        private static T _instance = null;

        public static T EnsureInstance()
        {
            if (_instance == null)
            {
                string filePath = getFilePathWithExtension(false);
                string resourceFilePath = Path.GetFileNameWithoutExtension(
                    filePath.Split(new string[]
                    {
                        "Resources"
                    }, StringSplitOptions.None).Last());

                if (Resources.Load(resourceFilePath) is not T instance)
                {
                    instance = CreateInstance<T>(); // note: in the debugger it might be displayed as null (which is not the case)

#if UNITY_EDITOR && !UNITY_CLOUD_BUILD
                    string completeFilePath = Path.Combine(Application.dataPath, filePath);
                    string directory = Path.GetDirectoryName(completeFilePath);
                    if (!Directory.Exists(directory))
                    {
                        if (directory == null)
                        {
                            return null;
                        }
                        
                        Directory.CreateDirectory(directory);
                    }

                    UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/" + filePath);
                    UnityEditor.AssetDatabase.Refresh();

#else
                    Debug.LogErrorFormat(
                        "Could not find scriptable object of type '{0}'. Make sure it is instantiated inside Unity before building.", 
                        typeof(T));
#endif
                }

                _instance = instance;
                _instance.OnAwake();
            }

            return _instance;
        }

        protected virtual void OnAwake() { }

        private static string getFilePathWithExtension(bool fullPath)
        {
            Type t = typeof(T);
            FieldInfo prop = t.GetField("FILE_PATH", BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic) ?? throw new($"No static Property 'FilePath' in {t}");

            // .. 하위 클래스에 식별자가 'FILE_PATH'인 전역 string type 필드가 선언되지 않았을 경우
            if (prop.GetValue(null) is not string filePath) throw new($"static property 'FILE_PATH' is not a string or null in {t}");
            // .. Resource를 통해 불러오므로 FILE_PATH에 Resources라는 경로가 포함되어 있어야 함 
            if (!filePath.Contains("Resources")) throw new("static property 'FILE_PATH' must contain a Resources folder.");
            // .. Plugins경로가 포함되어 있을 경우
            if (filePath.Contains("Plugins")) throw new("static property 'FILE_PATH' must not contain a Plugin folder.");

            // .. 스크립터블 오브젝트의 확장자는 .asset이므로 없다면 추가
            if (!filePath.EndsWith(".asset"))
            {
                filePath += ".asset";
            }

            return fullPath
                ? Path.Combine(Application.dataPath, filePath)
                : filePath;
        }
    }
}