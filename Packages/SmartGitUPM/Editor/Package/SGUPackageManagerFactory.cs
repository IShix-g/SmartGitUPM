
namespace SmartGitUPM.Editor
{
    internal sealed class SGUPackageManagerFactory
    {
        public SGUPackageManager Create()
        {
            var setting = UniqScriptableObject.CreateOrLoadAsset<PackageCollectionSetting>();
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