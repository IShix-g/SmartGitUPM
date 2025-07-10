
using System.Text.RegularExpressions;

namespace SmartGitUPM.Editor
{
    public class PackageInfoDetails
    {
        public PackageLocalInfo Local { get; private set; }
        public PackageRemoteInfo Remote { get; private set; }
        public string PackageInstallUrl { get; private set; }
        public bool HasUpdate { get; private set; }
        public bool IsInstalled => Local != default;
        public bool IsLoaded => Remote != default;
        public bool IsFixedVersion => !string.IsNullOrEmpty(FixedVersion);
        public string FixedVersion { get; private set; }

        public PackageInfoDetails(PackageLocalInfo local, PackageRemoteInfo remote, string packageInstallUrl)
        {
            Remote = remote;
            PackageInstallUrl = packageInstallUrl;
            Installed(local);
        }

        public void Installed(PackageLocalInfo info)
        {
            Local = info;
            Update();
        }

        public void UpdatePackageUrl(string packageInstallUrl)
        {
            PackageInstallUrl = packageInstallUrl;
            Update();
        }

        void Update()
        {
            FixedVersion = GetVersionParam();
            HasUpdate = HasUpdateInternal();
        }

        bool HasUpdateInternal()
        {
            try
            {
                if (!IsInstalled || !IsLoaded)
                {
                    return true;
                }

                if (IsFixedVersion)
                {
                    return Local.version != FixedVersion;
                }
                return Local.version != Remote.version;
            }
            catch
            {
                return false;
            }
        }

        public string GetVersionParam() => PackageInstallUrl.ToVersion();
    }
}
