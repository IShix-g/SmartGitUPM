
using System;
using System.Collections.Generic;

namespace SmartGitUPM.Editor
{
    [Serializable]
    internal sealed class PackageCacheInfos
    {
        public List<PackageCacheInfo> Packages = new ();
    }

    [Serializable]
    internal sealed class PackageCacheInfo
    {
        public string Name;
        public string DisplayName;
        public string Version;
        public string InstallUrl;

        public PackageCacheInfo(
            string installUrl,
            string name,
            string displayName,
            string version)
        {
            InstallUrl = installUrl;
            Name = name;
            DisplayName = displayName;
            Version = version;
        }
    }
}