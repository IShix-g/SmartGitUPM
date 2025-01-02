
using System;

namespace SmartGitUPM.Editor
{
    public sealed class UniquePackageCollectionSetting : UniqueScriptableObject<PackageCollectionSetting>
    {
        static readonly Lazy<UniquePackageCollectionSetting> s_instance
            = new (() => new UniquePackageCollectionSetting());
        
        UniquePackageCollectionSetting()
        {
            if (s_instance.IsValueCreated)
            {
                throw new InvalidOperationException($"This is a singleton class. Use {nameof(GetOrCreate)} to access the instance.");
            }
        }

        public static bool Has()
            => s_instance.Value.HasAsset();
        
        public static PackageCollectionSetting GetOrCreate()
            => s_instance.Value.CreateOrLoadAsset();
    }
}