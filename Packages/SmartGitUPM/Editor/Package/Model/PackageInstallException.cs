
using System;

namespace SmartGitUPM.Editor
{
    internal class PackageInstallException : Exception
    {
        public PackageInstallException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}