
using System;

namespace SmartGitUPM.Editor
{
    public class PackageInstallException : Exception
    {
        public PackageInstallException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}