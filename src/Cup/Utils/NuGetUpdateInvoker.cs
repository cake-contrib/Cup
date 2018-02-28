using System.Diagnostics;
using System.Net;
using Cup.Diagnostics;
using Spectre.System.IO;

namespace Cup.Utils
{
    public class NuGetUpdateInvoker
    {
        private readonly IFileSystem _filesystem;

        public NuGetUpdateInvoker(IFileSystem fileSystem)
        {
            _filesystem = fileSystem;
        }

        public bool Restore(IConsoleLog log, DirectoryPath working, FilePath file)
        {
            return RestorePackages(log, file, GetNuGetPath(working));
        }

        public bool Update(IConsoleLog log, DirectoryPath working, FilePath file, string package, string version)
        {
            return UpdatePackage(log, file, package, version, GetNuGetPath(working));
        }

        private FilePath GetNuGetPath(DirectoryPath working)
        {
            var nuget = working.CombineWithFilePath(new FilePath("nuget.exe"));
            if (!_filesystem.File.Exists(nuget))
            {
                var client = new WebClient();
                client.DownloadFile("https://dist.nuget.org/win-x86-commandline/latest/nuget.exe", nuget.FullPath);
            }
            return nuget;
        }

        private static bool RestorePackages(IConsoleLog log, FilePath file, FilePath nuget)
        {
            var info = new ProcessStartInfo(nuget.FullPath)
            {
                Arguments = $"restore {file.FullPath} -PackagesDirectory {file.GetDirectory().Combine(new DirectoryPath("../packages")).Collapse()}",
                RedirectStandardOutput = true
            };

            log.Write("Restoring packages for {0}...", file.FullPath);
            using (var process = Process.Start(info))
            {
                if (process != null)
                {
                    using (var reader = process.StandardOutput)
                    {
                        reader.ReadToEnd();
                    }
                    return process.ExitCode == 0;
                }
            }
            return false;
        }

        private static bool UpdatePackage(IConsoleLog log, FilePath file, string package, string version, FilePath nuget)
        {
            log.Write("Updating package {0} to {1}...", package, version);
            var info = new ProcessStartInfo(nuget.FullPath)
            {
                Arguments = $"update {file.FullPath} -Id {package} -Version {version} -RepositoryPath {file.GetDirectory().Combine(new DirectoryPath("../packages")).Collapse()}",
                RedirectStandardOutput = true
            };

            using (var process = Process.Start(info))
            {
                if (process != null)
                {
                    using (var reader = process.StandardOutput)
                    {
                        reader.ReadToEnd();
                    }
                    return process.ExitCode == 0;
                }
            }
            return false;
        }
    }
}
