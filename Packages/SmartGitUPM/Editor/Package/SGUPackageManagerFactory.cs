
namespace SmartGitUPM.Editor
{
    internal sealed class SGUPackageManagerFactory
    {
        public SGUPackageManager Create()
        {
            var setting = PackageCollectionSetting.LoadInstance();
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