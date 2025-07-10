
using System;
using System.Globalization;
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
        public bool IsFixedVersion { get; private set; }

        enum PreReleaseType { None, Beta, Alpha, Preview, Rc }

        public PackageInfoDetails(PackageLocalInfo local, PackageRemoteInfo remote, string packageInstallUrl)
        {
            Remote = remote;
            PackageInstallUrl = packageInstallUrl;
            Installed(local);
            IsFixedVersion = !string.IsNullOrEmpty(GetVersionParam());
        }

        public void Installed(PackageLocalInfo info)
        {
            Local = info;
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
                var local = ParseVersion(Local.version);
                var remote = ParseVersion(Remote.version);
                return CompareVersions(local, remote) < 0;
            }
            catch
            {
                return false;
            }
        }

        static (PreReleaseType PreReleaseType, int PreReleaseNumber, Version MainVersion) ParseVersion(string version)
        {
            var match = Regex.Match(version, @"^v(?<mainVersion>\d+\.\d+\.\d+)[-_]?(?<preReleaseType>beta|alpha|preview|rc)?_?(?<preReleaseNumber>\d+)?$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return (PreReleaseType.None, 0, new Version(version));
            }

            var mainVersionStr = match.Groups["mainVersion"].Value;
            var preReleaseTypeStr = match.Groups["preReleaseType"].Value.ToLower();
            var preReleaseNumberStr = match.Groups["preReleaseNumber"].Value;

            var mainVersion = new Version(mainVersionStr);
            var preReleaseType = PreReleaseType.None;
            var preReleaseNumber = 0;

            if (!string.IsNullOrEmpty(preReleaseTypeStr))
            {
                preReleaseType = preReleaseTypeStr switch
                {
                    "beta" => PreReleaseType.Beta,
                    "alpha" => PreReleaseType.Alpha,
                    "preview" => PreReleaseType.Preview,
                    "rc" => PreReleaseType.Rc,
                    _ => PreReleaseType.None,
                };
            }

            if (!string.IsNullOrEmpty(preReleaseNumberStr))
            {
                preReleaseNumber = int.Parse(preReleaseNumberStr, CultureInfo.InvariantCulture);
            }
            return (preReleaseType, preReleaseNumber, mainVersion);
        }

        static int CompareVersions(
            (PreReleaseType PreReleaseType, int PreReleaseNumber, Version MainVersion) local,
            (PreReleaseType PreReleaseType, int PreReleaseNumber, Version MainVersion) server
        )
        {
            var mainComparison = local.MainVersion.CompareTo(server.MainVersion);
            if (mainComparison != 0)
            {
                return mainComparison;
            }
            var preReleaseComparison = local.PreReleaseType.CompareTo(server.PreReleaseType);
            if (preReleaseComparison != 0)
            {
                return preReleaseComparison;
            }
            return local.PreReleaseNumber.CompareTo(server.PreReleaseNumber);
        }

        public string GetVersionParam()
            => GetVersionParam(PackageInstallUrl);

        public static string GetVersionParam(string packageInstallUrl)
        {
            var match = Regex.Match(packageInstallUrl, @"#v?([\d.]+)$");
            return match.Success
                ? match.Groups[1].Value
                : string.Empty;
        }
    }
}
