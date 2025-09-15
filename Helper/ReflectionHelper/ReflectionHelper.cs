using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jeomseon.Attribute;

namespace Jeomseon.Helper
{
    public static class ReflectionHelper
    {
        public static IEnumerable<T> CreateChildClassesFromType<T>() where T : class
        {
            Type baseType = typeof(T);

            foreach (Type type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()))
            {
                if (type.IsInterface || type.IsAbstract || !baseType.IsAssignableFrom(type)) continue;

                yield return Activator.CreateInstance(type) as T;
            }
        }

        public static IEnumerable<string> GetClassNamesFromParent(string baseClass)
        {
            return from assembly in AppDomain.CurrentDomain.GetAssemblies() 
                   let baseType = assembly.GetType(baseClass) 
                   where baseType is not null 
                   from type in assembly.GetTypes() 
                   where type.IsSubclassOf(baseType) && type.GetInterfaces().Contains(type) 
                   select type.Name;
        }

        public static IEnumerable<string> GetClassNamesFromParent<TBaseType>() where TBaseType : class
        {
            Type type = typeof(TBaseType);

            foreach (Type someType in AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()))
            {
                if (someType.IsAbstract ||
                    someType.IsInterface ||
                    !someType.IsSubclassOf(type) && !someType.GetInterfaces().Contains(type)) continue;

                yield return someType.Name;
            }
        }


        public static IEnumerable<Type> GetChildTypesFromBaseType(Type baseType)
        {
            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsInterface && !type.IsAbstract && baseType.IsAssignableFrom(type));
        }

        public static IEnumerable<Type> GetChildTypesFromBaseType<T>()
        {
            Type baseType = typeof(T);

            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsInterface && !type.IsAbstract && baseType.IsAssignableFrom(type));
        }

        public static IEnumerable<Type> GetChildClassesFromFieldTypeName(string typeName)
        {
            Type baseType = GetTypeFromFieldName(typeName);

            if (baseType is null)
            {
                Debug.LogWarning("Not Found BaseType!");
                return null;
            }

            return GetChildTypesFromBaseType(baseType);
        }

        public static Type GetTypeFromFieldName(string typeName)
        {
            string[] splitTypeNames = typeName.Split(' ', '.');

            if (splitTypeNames.Length <= 0)
            {
                Debug.LogWarning("typeName is Empty");
                return null;
            }

            string assemblyName = splitTypeNames[0];
            string baseTypeName = splitTypeNames[^1];

            Assembly targetAssembly = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SingleOrDefault(assembly => assembly.GetName().Name == assemblyName);

            if (targetAssembly is null)
            {
                Debug.LogWarning("Not Found Assembly!");
                return null;
            }

            Type baseType = targetAssembly
                .GetTypes()
                .SingleOrDefault(type => type.Name == baseTypeName);

            if (baseType is null)
            {
                Debug.LogWarning("Not Found BaseType!");
            }

            return baseType;
        }

        public static IEnumerable<string> GetEnumValuesFromEnumName(string enumTypeName)
        {
            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsEnum && type.Name == enumTypeName)
                .SelectMany(Enum.GetNames);
        }

        public static Dictionary<string, int> GetEnumKvpFromEnumName(string enumTypeName)
        {
            Dictionary<string, int> enumValues = new();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type enumType = assembly
                    .GetTypes()
                    .FirstOrDefault(t => t.IsEnum && t.Name == enumTypeName);

                if (enumType is not null)
                {
                    Type underlyingType = Enum.GetUnderlyingType(enumType);

                    foreach (string name in Enum.GetNames(enumType))
                    {
                        object enumValue = Enum.Parse(enumType, name);
                        int value = Convert.ToInt32(Convert.ChangeType(enumValue, underlyingType));
                        enumValues.Add(name, value);
                    }

                    break;
                }
            }

            return enumValues;
        }
    }
}
