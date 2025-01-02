
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SmartGitUPM.Editor.Localization
{
    [UniqueScriptableObjectPath("Assets/Editor/SmartGitUPM_LocalizationSetting.asset")]
    public sealed class LocalizationSetting : ScriptableObject
    {
        public event Action<SystemLanguage> OnLanguageChanged = delegate { };
        
        [SerializeField,
         OnValueChanged("OnValueChanged"),
         DynamicDropdown("GetSupportedLanguages")]
        SystemLanguage _language = SystemLanguage.English;
        
        #region Editor
        void OnValueChanged() => OnLanguageChanged(_language);
        object[] GetSupportedLanguages() => _supportedLanguages;
        #endregion
        
        public SystemLanguage Language => _language;
        
        object[] _supportedLanguages;
        
        public void SetSupportedLanguage(IReadOnlyList<SystemLanguage> languages)
        {
            _supportedLanguages = new object[languages.Count];
            for (var i = 0; i < languages.Count; i++)
            {
                _supportedLanguages[i] = languages[i];
            }
        }

        public void SetLanguage(SystemLanguage language)
        {
            if (_language == language)
            {
                return;
            }
            _language = language;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
            OnLanguageChanged(_language);
        }
    }
}