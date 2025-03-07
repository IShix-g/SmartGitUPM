
using UnityEditor;
using UnityEngine;

namespace SmartGitUPM.Editor.Localization
{
    internal static class LanguageManagerFactory
    {
        const string _dataPath = PackageCollectionWindow.PackagePath + "Editor/LocalizationData.asset";

        static LanguageManager s_manager;
        static LocalizationSetting s_setting;
        static LocalizationData s_data;

        public static LanguageManager GetOrCreate()
        {
            if (s_manager == default
                || s_manager.IsDisposed)
            {
                s_manager = Create();
            }
            return s_manager;
        }

        static LanguageManager Create()
        {
            InitializeLocalization();
            return Create(s_data, s_setting);
        }

        public static LanguageManager Create(LocalizationData data, LocalizationSetting setting)
        {
            var manager = new LanguageManager(data, setting.Language);
            setting.OnLanguageChanged -= OnLanguageChanged;
            setting.OnLanguageChanged += OnLanguageChanged;
            return manager;
        }

        static void OnLanguageChanged(SystemLanguage language)
            => s_manager?.SetLanguage(language);

        static void InitializeLocalization()
        {
            var isSettingNew = InitializeSetting();
            var isDataNew = InitializeData();
            if (isSettingNew || isDataNew)
            {
                SetLanguageDependencies(s_setting, s_data);
            }
        }

        static bool InitializeSetting()
        {
            if (s_setting != default)
            {
                return false;
            }

            var isNew = !UniqueLocalizationSetting.Has();
            s_setting = UniqueLocalizationSetting.GetOrCreate();
            if (isNew)
            {
                var currentSystemLanguage = Application.systemLanguage;
                if (s_setting.Language != currentSystemLanguage)
                {
                    s_setting.SetLanguage(currentSystemLanguage);
                    EditorUtility.SetDirty(s_setting);
                    AssetDatabase.SaveAssetIfDirty(s_setting);
                }
            }
            return isNew;
        }

        static bool InitializeData()
        {
            if (s_data != default)
            {
                return false;
            }

            s_data = AssetDatabase.LoadAssetAtPath<LocalizationData>(_dataPath);

            if (s_data == default)
            {
                Debug.LogError($"Failed to load LocalizationData at path: {_dataPath}");
                return false;
            }
            return true;
        }

        static void SetLanguageDependencies(LocalizationSetting setting, LocalizationData data)
        {
            setting.OnLanguageChanged -= data.SetLanguage;
            setting.OnLanguageChanged += data.SetLanguage;
            setting.SetSupportedLanguage(data.SupportedLanguages);
            data.SetLanguage(setting.Language);
        }
    }
}