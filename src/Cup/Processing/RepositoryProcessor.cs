using System;
using Cup.Diagnostics;
using Spectre.System;
using Spectre.System.IO;

namespace Cup.Processing
{
    public class RepositoryProcessor
    {
        private readonly IFileSystem _filesystem;
        private readonly IEnvironment _environment;
        private readonly IConsoleLog _log;

        public RepositoryProcessor(IFileSystem fileSystem, IEnvironment environment, IConsoleLog log)
        {
            _filesystem = fileSystem;
            _environment = environment;
            _log = log;
        }

        public int Process(DirectoryPath root, DirectoryPath repository, string version)
        {
            _log.Write("Processing packages.config files...");
            var packageProcessor = new PackageProcessor(_filesystem, _environment, _log);
            var processed = packageProcessor.Process(root, repository, version);
            if (processed == 0)
            {
                _log.Error("Nothing was updated. Probably a newer repository.");
            }

            return processed;
        }
    }
}
