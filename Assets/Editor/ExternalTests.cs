
using UnityEditor;
using UnityEngine;
using SmartGitUPM.Editor;

namespace Editor
{
    public sealed class ExternalTests
    {
        [MenuItem("Tests/External/Load Settings")]
        public static void LoadSettingsTest()
        {
            var setting = UniquePackageCollectionSetting.GetOrCreate();
            Debug.Log(setting.Length);
        }
    }
}