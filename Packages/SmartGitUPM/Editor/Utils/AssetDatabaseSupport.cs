
using System.IO;
using UnityEngine;
using UnityEditor;

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
            
            CreateDirectories(path);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }
        
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

        public static void CreateDirectories(string path)
        {
            if (!path.StartsWith("Assets"))
            {
                Debug.LogError("The path must start with 'Assets/' ");
                return;
            }

            path = Path.HasExtension(path)
                ? Path.GetDirectoryName(path)
                : path;
            path = path?.Replace("\\", "/");

            if (string.IsNullOrEmpty(path)
                || AssetDatabase.IsValidFolder(path))
            {
                return;
            }
    
            var folders = path.Split('/');
            var parentFolder = folders[0];
    
            for (var i = 1; i < folders.Length; i++)
            {
                var newFolder = parentFolder + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newFolder))
                {
                    AssetDatabase.CreateFolder(parentFolder, folders[i]);
                }
                parentFolder = newFolder;
            }
        }
    }
}