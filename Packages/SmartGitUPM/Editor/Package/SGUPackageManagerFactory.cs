
namespace SmartGitUPM.Editor
{
    internal static class SGUPackageManagerFactory
    {
        public static SGUPackageManager Create()
        {
            var setting = UniquePackageCollectionSetting.GetOrCreate();
            var installer = new PackageInstaller();
            var infoFetchers = new IPackageInfoFetcher[]
            {
                new HttpPackageInfoFetcher(installer),
                new SshPackageInfoFetcher(installer)
            };
            return new SGUPackageManager(installer, infoFetchers, setting);
        }
    }
}