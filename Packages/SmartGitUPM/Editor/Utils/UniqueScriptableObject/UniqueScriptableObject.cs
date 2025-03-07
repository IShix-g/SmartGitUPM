
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SmartGitUPM.Editor
{
    public abstract class UniqueScriptableObject<T> where T : ScriptableObject
    {
        string _cachedPath;
        T _cachedAsset;

        protected virtual bool OnCreated(T setting) => false;
        protected virtual bool OnLoaded(T asset) => false;

        public bool HasAsset() => File.Exists(ValidateAndGetPath());

        public string GetAssetPath()
        {
            if (!string.IsNullOrEmpty(_cachedPath))
            {
                return _cachedPath;
            }
            var attr = (UniqueScriptableObjectPathAttribute) Attribute.GetCustomAttribute(typeof(T), typeof(UniqueScriptableObjectPathAttribute));
            if (attr == default)
            {
                throw new InvalidOperationException($"{nameof(UniqueScriptableObjectPathAttribute)} is not defined on the class {typeof(T)}.");
            }
            _cachedPath = attr.Path;
            return _cachedPath;
        }

        public T LoadAsset() => LoadAsset(ValidateAndGetPath());

        public T LoadAsset(string path)
        {
            if (_cachedAsset != default)
            {
                return _cachedAsset;
            }
            _cachedAsset = AssetDatabaseSupport.LoadAsset<T>(path);
            if (_cachedAsset != default
                && OnLoaded(_cachedAsset))
            {
                EditorUtility.SetDirty(_cachedAsset);
                AssetDatabase.SaveAssetIfDirty(_cachedAsset);
            }
            return _cachedAsset;
        }

        public T CreateOrLoadAsset()
        {
            var path = ValidateAndGetPath();
            var asset = LoadAsset(path);
            if (asset != default)
            {
                return asset;
            }
            asset = AssetDatabaseSupport.CreateAsset<T>(path);
            if (OnCreated(asset))
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
            }
            return LoadAsset(path);
        }
 
        string ValidateAndGetPath()
        {
            var path = GetAssetPath();
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException("The specified path is null or empty.");
            }
            if (!path.EndsWith(".asset"))
            {
                throw new ArgumentException($"The path must end with '.asset'. Provided path: {path}");
            }
            return path;
        }
    }
}