
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CMSuniVortex.Tasks;
using UnityEditor;
using SmartGitUPM.Editor;
using UnityEngine;
using UnityEngine.Networking;

namespace Editor
{
    public class TestWindow
    {
        [MenuItem("Tests/Create Path")]
        public static void CreatePath()
        {
            AssetDatabaseSupport.CreateDirectories("Assets/Test/Test2");
        }
        
        [MenuItem("Tests/Install Package")]
        public static void InstallPackage()
        {
            var installer = new PackageInstaller();

            installer.Install(new[]
                {
                    "https://github.com/IShix-g/CMSuniVortex.git?path=Packages/CMSuniVortex",
                    "https://github.com/IShix-g/SMCP-Configurator.git?path=Packages/SMCPConfigurator#1.0.9"
                })
                .Handled(task =>
                {
                    if (task.IsFaulted)
                    {
                        throw task.Exception;
                    }

                    var details = task.Result;
                    Debug.Log(details.Select(x => x.name + " " + x.version).Aggregate((a, b) => a + "\n" + b));
                });
        }
        
        [MenuItem("Tests/Execute Git Clone")]
        public static void ExecuteGitClone()
        {
            var installer = new PackageInstaller();
            var fetcher = new SshPackageInfoFetcher(installer);
            var packageSsh = "git@IShix-g-GitHub:IShix-g/UnityJenkinsBuilder.git?path=Assets/Plugins/Jenkins";
            var branch = "main";
            
            fetcher.FetchPackageInfo(packageSsh, branch, true)
                .Handled(task =>
                {
                    if (task.IsFaulted)
                    {
                        throw task.Exception;
                    }
                    
                    var details = task.Result;
                    Debug.Log("Server: \n" + JsonUtility.ToJson(details.Remote, true) + "\nLocal: \n" + JsonUtility.ToJson(details.Local, true));
                });
        }

        [MenuItem("Tests/Update Check")]
        static void UpdateCheck()
        {
            var updater = new PackageUpdateChecker();
            updater.CheckUpdate()
                .Handled();
        }

        [MenuItem("Tests/Open Dialog")]
        static void OpenDialog()
        {
            var contents = new CustomDialogContents(
                    "System Update Required",
                    "Your system requires an important update to improve performance and security.",
                    () => Debug.Log("Clicked Yes"),
                    "Web",
                    "Close",
                    isClickedYes => Debug.Log("Closed. Clicked Yes: " + isClickedYes)
                );
            CustomDialog.Open(contents, default, "Notice");
        }

        [MenuItem("Tests/Print Package Cache")]
        static void PrintPackageCache()
        {
            PackageCacheManager.Initialize();
            if (PackageCacheManager.HasCache())
            {
                Debug.Log(PackageCacheManager.Infos.Packages.Select(x => "- " + x.DisplayName + " (" + x.Name + ")").Aggregate((a,b) => a + "\n" + b));
            }
        }

        [MenuItem("Tests/Check Default Branch")]
        static void GetDefaultBranch()
        {
            var owner = "IShix-g";
            var repo = "CMSuniVortex";
            GetDefaultBranchAsync(owner, repo).Handled(task =>
            {
                Debug.Log("Defaultbranch is " + task.Result);
            });
        }
        
        public static async Task<string> GetDefaultBranchAsync(string owner, string repo)
        {
            // README.md のプレースホルダを利用するURLを使用
            var url = $"https://github.com/{owner}/{repo}/blob/HEAD/README.md";
            using var httpClient = new HttpClient();

            try
            {
                // HTTPヘッダーからリダイレクト先を見る
                var response = await httpClient.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.Redirect ||
                    response.StatusCode == System.Net.HttpStatusCode.MovedPermanently)
                {
                    var redirectUrl = response.Headers.Location.ToString();
                    if (redirectUrl.Contains("/blob/"))
                    {
                        // リダイレクトURL内の "/blob/{branch}" の部分からブランチ名を抽出
                        var parts = redirectUrl.Split(new[] { "/blob/" }, StringSplitOptions.None);
                        if (parts.Length > 1)
                        {
                            var branchPart = parts[1].Split('/')[0];
                            return branchPart;
                        }
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Debug.Log("No redirection detected, HEAD might not be resolvable this way.");
                }
                else
                {
                    Debug.Log($"Unexpected status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Error fetching default branch: {ex.Message}");
            }

            return null;
        }
    }
}