using System.Threading;
using System.Threading.Tasks;

namespace SmartGitUPM.Editor
{
    /// <summary>
    /// Manages packages registered in Smart Git UPM without opening Editor UI.
    /// Call and await these methods sequentially from the Editor main thread.
    /// </summary>
    public static class SmartGitUPMApi
    {
        /// <summary>Checks whether any registered package is not installed.</summary>
        public static async Task<bool> HasUninstalledPackagesAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            using var manager = SGUPackageManagerFactory.Create();
            return await manager.HasUninstalledPackagesAsync(token);
        }

        /// <summary>Fetches current remote information and checks installed packages for updates.</summary>
        public static async Task<bool> HasUpdatesAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            using var manager = SGUPackageManagerFactory.Create();
            return await manager.HasUpdatesAsync(token);
        }

        /// <summary>Updates a package using its complete registered install URL, including any revision.</summary>
        public static async Task UpdatePackageAsync(string packageInstallUrl, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            using var manager = SGUPackageManagerFactory.Create();
            await manager.UpdatePackageAsync(packageInstallUrl, token);
        }

        /// <summary>Installs or updates all registered packages, including packages with notifications disabled.</summary>
        public static async Task InstallAllAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            using var manager = SGUPackageManagerFactory.Create();
            await manager.InstallAllAsync(token);
        }
    }
}
