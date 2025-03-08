
using System;

namespace SmartGitUPM.Editor.Localization
{
    internal sealed class UniqueLocalizationSetting : UniqueScriptableObject<LocalizationSetting>
    {
        static readonly Lazy<UniqueLocalizationSetting> s_instance
            = new (() => new UniqueLocalizationSetting());

        UniqueLocalizationSetting()
        {
            if (s_instance.IsValueCreated)
            {
                throw new InvalidOperationException($"This is a singleton class. Use {nameof(GetOrCreate)} to access the instance.");
            }
        }

        public static bool Has()
            => s_instance.Value.HasAsset();

        public static LocalizationSetting GetOrCreate()
            => s_instance.Value.CreateOrLoadAsset();
    }
}
