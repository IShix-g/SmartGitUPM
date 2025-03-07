
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SmartGitUPM.Editor.Localization
{
    internal sealed class LocalizationData : ScriptableObject
    {
        [SerializeField] List<LocalizationEntry> _localizedLanguages;

        public bool IsEmpty => _localizedLanguages is null
                               || _localizedLanguages.Count == 0;

        public SystemLanguage[] SupportedLanguages
        {
            get
            {
                if (_supportedLanguages is null
                    || _supportedLanguages.Length == 0)
                {
                    _supportedLanguages = _localizedLanguages is {Count: > 0}
                        ? _localizedLanguages[0].Entries
                            .Select(x => x.Language)
                            .Distinct()
                            .ToArray()
                        : Array.Empty<SystemLanguage>();
                }
                return _supportedLanguages;
            }
        }

        SystemLanguage[] _supportedLanguages;

        public void SetLanguage(SystemLanguage language)
        {
            language = ToSupportedLanguage(language);
            foreach (var entry in _localizedLanguages)
            {
                if (entry.CanSetLanguage(language))
                {
                    entry.SetLanguage(language);
                }
            }
        }

        public SystemLanguage ToSupportedLanguage(SystemLanguage language)
        {
            foreach (var lang in SupportedLanguages)
            {
                if (lang == language)
                {
                    return language;
                }
            }
            return SupportedLanguages[0];
        }

        public LocalizationEntry GetEntry(string key)
            => _localizedLanguages.Find(x => x.Key == key);

        public bool TryGetEntry(string key, out LocalizationEntry value)
        {
            value = GetEntry(key);
            return value != default;
        }
    }

    [Serializable]
    public class LocalizationEntry
    {
        public string Key;
        public List<LocalizedText> Entries;
        public LocalizedText Text { get; private set; }
        public string CurrentValue => Text?.Value ?? string.Empty;

        public bool CanSetLanguage(SystemLanguage language)
            => Text == default
               || Text.Language != language;

        public void SetLanguage(SystemLanguage language)
        {
            Text = Entries?.Find(x => x.Language == language)
                   ?? Entries?.FirstOrDefault();
            if (Text == default)
            {
                Debug.LogWarning($"Translation missing for key '{Key}' in language '{language}'");
            }
        }

        public string GetTranslation() => CurrentValue;

        public bool TryGetTranslation(out string value)
        {
            value = CurrentValue;
            return value != string.Empty;
        }

        public string GetTranslation(SystemLanguage language)
        {
            var value = Entries.Find(x => x.Language == language)?.Value;
            if (string.IsNullOrEmpty(value))
            {
                throw new Exception($"Missing translation for {Key} in {language}");
            }
            return value;
        }

        public bool TryGetTranslation(SystemLanguage language, out string value)
        {
            value = GetTranslation(language);
            return !string.IsNullOrEmpty(value);
        }
    }

    [Serializable]
    public class LocalizedText
    {
        public SystemLanguage Language;
        public string Value;
    }
}