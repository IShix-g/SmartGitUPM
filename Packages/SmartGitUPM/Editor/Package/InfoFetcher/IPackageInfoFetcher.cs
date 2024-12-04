
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartGitUPM.Editor
{
    public interface IPackageInfoFetcher : IDisposable
    {
        public bool IsProcessing{ get; }
        public string SupportProtocol { get; }
        public bool IsSupported(string protocol);
        public Task<PackageInfoDetails> FetchPackageInfo(string packageInstallUrl, string branch, bool supperReload, CancellationToken token = default);
    }
}