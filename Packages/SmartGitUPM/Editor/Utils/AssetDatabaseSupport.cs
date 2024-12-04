
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using JetBrains.Annotations;

namespace SmartGitUPM.Editor
{
    internal static class AssetDatabaseSupport
    {
        public static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var asset = LoadAsset<T>(path);
            if (asset != default)
            {
                return asset;
            }
            
            CreateDirectory(path);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }
        
        [CanBeNull]
        public static T LoadAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != default)
            {
                return asset;
            }

            var dir = Path.GetDirectoryName(path);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                return default;
            }
            var currentGuids = AssetDatabase.FindAssets("t:" + typeof(T), new[] { dir });
            if (currentGuids is {Length: > 0})
            {
                path = AssetDatabase.GUIDToAssetPath(currentGuids[0]);
                asset = AssetDatabase.LoadAssetAtPath<T>(path);
            }
            return asset;
        }

        public static void CreateDirectory(string path) 
        {
            if (!path.StartsWith("Assets"))
            {
                throw new ArgumentException("Specify a path starting with Assets.");
            }
            
            var directory = Path.HasExtension(path)
                    ? Path.GetDirectoryName(path)
                    : path;

            if (AssetDatabase.IsValidFolder(directory))
            {
                return;
            }
            
            var parentFolder = "Assets";
            var folders = directory.Substring(parentFolder.Length).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var folder in folders)
            {
                var newFolder = Path.Combine(parentFolder, folder);
                if (!AssetDatabase.IsValidFolder(newFolder)) 
                {
                    AssetDatabase.CreateFolder(parentFolder, folder);
                }
                parentFolder = newFolder;   
            }
        }
    }
}