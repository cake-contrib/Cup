using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cup.Infrastructure.Diagnostics;
using Spectre.System;
using Spectre.System.IO;

namespace Cup.Commands.Update.Processing
{
    public class PackageProcessor
    {
        private const string Pattern = @"<package id=""(?<package>[A-Za-z0-9\.]*)"" version=""(?<version>[0-9]*\.[0-9]*\.[0-9]*)"" targetFramework=""(?<framework>[a-z0-9]*)"" \/>";

        private readonly IFileSystem _filesystem;
        private readonly IEnvironment _environment;
        private readonly IConsoleLog _log;
        private readonly IGlobber _globber;
        private readonly NuGetTool _nuget;

        public PackageProcessor(IFileSystem fileSystem, IEnvironment environment, IConsoleLog log)
        {
            _filesystem = fileSystem;
            _environment = environment;
            _log = log;
            _globber = new Globber(fileSystem, environment);
            _nuget = new NuGetTool(fileSystem);
        }

        public int Process(DirectoryPath root, DirectoryPath repository, string version)
        {
            var comparer = new PathComparer(_environment);
            var regex = new Regex(Pattern);

            var toolPackages = repository.CombineWithFilePath(new FilePath("tools/packages.config"));
            var packageFiles = _globber.Match("**/packages.config", new GlobberSettings
            {
                Root = repository,
                Comparer = comparer
            }).OfType<FilePath>();

            var processed = 0;
            foreach (var packageFile in packageFiles)
            {
                _log.Write("Processing {0}...", packageFile);

                using (var scope = _log.Indent())
                {
                    if (comparer.Equals(packageFile, toolPackages))
                    {
                        scope.Write("Skipping packages.config in tools.");
                        continue;
                    }

                    if (!_nuget.Restore(scope, root, packageFile))
                    {
                        scope.Error("Could not restore NuGet package.");
                        throw new InvalidOperationException("An error occured when restoring packages!");
                    }

                    // Read the file contents.
                    string contents;
                    using (var stream = _filesystem.File.OpenRead(packageFile))
                    using (var reader = new StreamReader(stream))
                    {
                        contents = reader.ReadToEnd();
                    }

                    // Find all packages that need to be updated.
                    var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var match = regex.Match(contents);
                    if (match.Success)
                    {
                        while (match.Success)
                        {
                            var package = match.Groups["package"].Value;
                            if (package == "Cake.Core" || package == "Cake.Testing")
                            {
                                result.Add(package);
                            }
                            match = match.NextMatch();
                        }
                        processed++;
                    }

                    foreach (var package in result)
                    {
                        if (!_nuget.Update(scope, root, packageFile, package, version))
                        {
                            scope.Error("Could not update package '{0}' in file '{1}'.", package, packageFile);
                            throw new InvalidOperationException("An error occured when updating package!");
                        }
                    }
                }
            }

            return processed;
        }
    }
}
