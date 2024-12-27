
using System;
using System.Diagnostics;
using UnityEngine;

namespace SmartGitUPM.Editor
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal class UniqScriptableObjectPathAttribute : Attribute
    {
        public string Path { get; }

        public UniqScriptableObjectPathAttribute(string path)
        {
            Path = path;
        }
    }
    
    public static class UniqScriptableObject
    {
        public static string GetAssetPath<T>() where T : ScriptableObject
        {
            var attr = (UniqScriptableObjectPathAttribute) Attribute.GetCustomAttribute(typeof(T), typeof(UniqScriptableObjectPathAttribute));
            return attr?.Path ?? throw new InvalidOperationException($"UniqScriptableObjectPathAttribute is not defined on the class {typeof(T)}.");
        }
        
        public static T LoadAsset<T>() where T : ScriptableObject
        {
            var path = GetAssetPath<T>();
            if (!path.EndsWith(".asset"))
            {
                throw new ArgumentException("Specify the path to the file with extension. path: " + path);
            }
            return AssetDatabaseSupport.LoadAsset<T>(path);
        }
        
        public static T CreateOrLoadAsset<T>() where T : ScriptableObject
        {
            var path = GetAssetPath<T>();
            if (!path.EndsWith(".asset"))
            {
                throw new ArgumentException("Specify the path to the file with extension. path: " + path);
            }
            return AssetDatabaseSupport.CreateOrLoad<T>(path);
        }
    }
}