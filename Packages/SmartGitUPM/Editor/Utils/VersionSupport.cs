
using System.Text.RegularExpressions;

namespace SmartGitUPM.Editor
{
    public static class VersionSupport
    {
        public static string WithoutVersion(this string url)
            => Regex.Replace(url, @"[#@][^\s?#]*(\d+\.\d+\.\d+)", string.Empty);

        public static string ToVersion(this string url)
        {
            var match = Regex.Match(url, @"[#@][^\s?#]*(\d+\.\d+\.\d+)");
            return match.Success
                ? match.Groups[1].Value
                : string.Empty;
        }
    }
}
