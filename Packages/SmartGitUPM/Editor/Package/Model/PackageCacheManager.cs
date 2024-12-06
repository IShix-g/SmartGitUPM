
using UnityEditor;
using UnityEngine;

namespace SmartGitUPM.Editor
{
    public sealed class PackageCacheManager
    {
        const string _editorPrefsKey = "PackageCacheInfoKey";

        public static PackageCacheInfos Infos { get; private set; }
        
        public static void Initialize() => Infos ??= Load() ?? new PackageCacheInfos();
        
        public static bool HasKey() => EditorPrefs.HasKey(_editorPrefsKey);

        public static bool HasCacheByName(string name) => TryGetByName(name, out _);
        
        public static bool HasCacheByInstallUrl(string installUrl) => TryGetByInstallUrl(installUrl, out _);
        
        public static bool HasCache() => Infos != default && Infos.Packages.Count > 0;
        
        public static void Save() => Save(Infos);
        
        public static void Save(PackageCacheInfos infos)
        {
            var json = JsonUtility.ToJson(infos);
            EditorPrefs.SetString(_editorPrefsKey, json);
        }
        
        public static PackageCacheInfos Load()
        {
            var json = EditorPrefs.GetString(_editorPrefsKey);
            return JsonUtility.FromJson<PackageCacheInfos>(json);
        }
        
        public static bool TryGetByName(string name, out PackageCacheInfo info)
        {
            info = GetByName(name);
            return info != default;
        }
        
        public static PackageCacheInfo GetByName(string name)
            => Infos.Packages.Find(info => info.Name == name);
        
        public static bool TryGetByInstallUrl(string installUrl, out PackageCacheInfo info)
        {
            info = GetByInstallUrl(installUrl);
            return info != default;
        }
        
        public static PackageCacheInfo GetByInstallUrl(string installUrl)
            => Infos.Packages.Find(info => info.InstallUrl == installUrl);

        public static void Add(PackageCacheInfo info)
        {
            var index = Infos.Packages.FindIndex(x => x.Name == info.Name);
            if (index >= 0)
            {
                Infos.Packages[index] = info;
            }
            else
            {
                Infos.Packages.Add(info);
            }
        }

        public static void Remove(string name)
        {
            var index = Infos.Packages.FindIndex(info => info.Name == name);
            if (index >= 0)
            {
                Infos.Packages.RemoveAt(index);
            }
        }
        
        public static void Remove(PackageCacheInfo info)
        {
            Infos.Packages.Remove(info);
        }

        public static void DeleteAll()
        {
            Infos = new PackageCacheInfos();
            EditorPrefs.DeleteKey(_editorPrefsKey);
        }
    }
}