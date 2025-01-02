
using SmartGitUPM.Editor.Localization;
using UnityEditor;
using UnityEngine;

namespace SmartGitUPM.Editor
{
    internal sealed class EditorInitializationExecuter
    {
        const string _key = "SmartGitUPM_EditorInitializationExecuter_FirstInit";
        
        public static bool IsFirstInit
        {
            get => SessionState.GetBool(_key, false);
            set => SessionState.SetBool(_key, value);
        }
        
        [InitializeOnLoadMethod]
        static void DetectEditorStartup()
        {
            if (!IsFirstInit)
            {
                FirstInit();
                IsFirstInit = true;
            }

            OnProjectLoadedInEditor();
        }

        static void FirstInit()
        {
            var languageManager = LanguageManagerFactory.GetOrCreate();
            var strings = GetLocalizedStrings(languageManager);
            var updater = new PackageUpdateChecker(strings);
            updater.CheckUpdate().Handled(_ => updater.Dispose());
        }

        public static PackageUpdateChecker.LocalizedStrings GetLocalizedStrings(LanguageManager manager)
        {
            return new PackageUpdateChecker.LocalizedStrings
            {
                Title = manager.GetEntry("UpdateChecker/Title").CurrentValue,
                Button = manager.GetEntry("UpdateChecker/Button").CurrentValue,
                Installed = manager.GetEntry("UpdateChecker/NotInstalled").CurrentValue,
                UpdateAvailable = manager.GetEntry("UpdateChecker/UpdateAvailable").CurrentValue
            };
        }
        
        static void OnProjectLoadedInEditor()
            => PackageCacheManager.Initialize();
    }
}