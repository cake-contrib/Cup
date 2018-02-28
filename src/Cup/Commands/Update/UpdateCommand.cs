using System;
using System.Diagnostics;
using System.Linq;
using Cup.Commands.Update.Processing;
using Cup.Infrastructure.Diagnostics;
using JetBrains.Annotations;
using LibGit2Sharp;
using Octokit;
using Spectre.CommandLine;
using Spectre.System;
using Spectre.System.IO;
using GitCommands = LibGit2Sharp.Commands;
using GitRepository = LibGit2Sharp.Repository;
using GitSignature = LibGit2Sharp.Signature;
using OctokitRepository = Octokit.Repository;

namespace Cup.Commands.Update
{
    [UsedImplicitly]
    public class UpdateCommand : Command<UpdateSettings>
    {
        private readonly IFileSystem _filesystem;
        private readonly IEnvironment _environment;
        private readonly IConsoleLog _log;
        private readonly RepositoryProcessor _processor;

        public UpdateCommand(IFileSystem filesystem, IEnvironment environment, IConsoleLog log, RepositoryProcessor processor)
        {
            _filesystem = filesystem;
            _environment = environment;
            _log = log;
            _processor = processor;
        }

        public override int Execute(UpdateSettings settings, ILookup<string, string> remaining)
        {
            // Get the user.
            var client = new GitHubClient(new ProductHeaderValue("Cake-Addin-Updater"))
            {
                Credentials = new Octokit.Credentials(settings.Token)
            };

            // Validate the provided version.
            if (!System.Version.TryParse(settings.Version, out var _))
            {
                _log.Error("The provided version is not valid.");
                return 1;
            }

            // Get the root.
            var root = settings.WorkingDirectory ?? new DirectoryPath(".");
            root = root.MakeAbsolute(_environment);

            // Get the user.
            var user = client.User.Get(settings.User).Result;
            var userEmail = GetUserEmail(client);

            // Get the repository parts.
            var info = GetRepositoryInfo(settings);

            // Does the directory contains anything?
            var path = CreateRepositoryDirectory(root, info);
            if (_filesystem.Directory.GetFiles(path, "*.*", SearchScope.Current).Any() ||
               _filesystem.Directory.GetDirectories(path, "*.*", SearchScope.Current).Any())
            {
                _log.Error($"Repository '{path}' already exist on disk.");
                _log.Write("Remove it and try again.");
                return 1;
            }

            // Fork the repository.
            var repository = ForkRepository(client, info);
            if (string.IsNullOrWhiteSpace(repository?.Name))
            {
                _log.Error("Could not fork repository.");
                return 1;
            }

            // Get the default branch.
            var defaultBranch = repository.DefaultBranch;
            if (string.IsNullOrWhiteSpace(defaultBranch))
            {
                _log.Error("Could not get default branch for repository.");
                return 1;
            }

            // Clone the repository at the specified path.
            _log.Write("Cloning repository...");
            GitRepository.Clone($"https://github.com/{settings.User}/{repository.Name}", path.FullPath, new CloneOptions
            {
                Checkout = true
            });

            using (var gitRepository = new GitRepository(path.FullPath))
            {
                // Create a new branch in the repository.
                _log.Write("Creating branch...");
                gitRepository.CreateBranch($"feature/cake-{settings.Version}");
                _log.Write("Checking out branch...");
                GitCommands.Checkout(gitRepository, $"feature/cake-{settings.Version}");

                // Update all package references in project.
                var processed = _processor.Process(root, path, settings.Version);
                if (processed == 0)
                {
                    _log.Error("Nothing was updated. Probably a newer repository.");
                    return 1;
                }

                // Commit?
                if (settings.Commit)
                {
                    _log.Write("Staging changes...");
                    GitCommands.Stage(gitRepository, "*");

                    var status = gitRepository.RetrieveStatus();
                    if (status.Any())
                    {
                        _log.Write("Committing changes...");
                        var author = new GitSignature(user.Name, userEmail, DateTime.Now);
                        gitRepository.Commit($"Updated to Cake {settings.Version}.", author, author);

                        // Push?
                        if (settings.Push)
                        {
                            // Build everything first.
                            if (!BuildProject(path))
                            {
                                return 1;
                            }

                            // Push the commit.
                            if (!Push(settings, path))
                            {
                                return 1;
                            }

                            // Create a pull request?
                            if (settings.OpenPullRequest)
                            {
                                CreatePullRequest(client, settings, info);
                            }
                        }
                    }
                    else
                    {
                        _log.Error("No changes in repository. Already updated?");
                    }
                }
            }

            return 0;
        }

        private string GetUserEmail(GitHubClient client)
        {
            var emails = client.User.Email.GetAll().Result;
            var email = emails.FirstOrDefault(x => x.Primary)?.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                _log.Error("Could not resolve email for user.");
                return null;
            }
            return email;
        }

        private static (string owner, string repository) GetRepositoryInfo(UpdateSettings settings)
        {
            var parts = settings.Repository.Trim().Split('/');
            var owner = parts[0]?.Trim();
            if (string.IsNullOrWhiteSpace(owner))
            {
                throw new InvalidOperationException("Could not parse repository owner.");
            }
            var repository = parts[1]?.Trim();
            if (string.IsNullOrWhiteSpace(owner))
            {
                throw new InvalidOperationException("Could not parse repository name.");
            }
            return (owner, repository);
        }

        private DirectoryPath CreateRepositoryDirectory(DirectoryPath path, (string owner, string name) info)
        {
            path = path.Combine(new DirectoryPath(info.name));
            if (!_filesystem.Directory.Exists(path))
            {
                _log.Write("Creating directory for repository...");
                _filesystem.Directory.Create(path);
            }
            return path;
        }

        private OctokitRepository ForkRepository(GitHubClient client, (string owner, string name) info)
        {
            _log.Write("Forking repository...");
            return client.Repository.Forks.Create(info.owner, info.name, new NewRepositoryFork()).Result;
        }

        private bool BuildProject(DirectoryPath path)
        {
            var info = new ProcessStartInfo("powershell");
            var buildScript = path.CombineWithFilePath(new FilePath("build.ps1"));
            info.Arguments = $"-f {buildScript}";
            info.WorkingDirectory = path.FullPath;

            var process = Process.Start(info);
            if (process == null)
            {
                return false;
            }

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                _log.Error("Something went wrong when building.");
                return false;
            }

            return true;
        }

        private bool Push(UpdateSettings settings, DirectoryPath path)
        {
            var info = new ProcessStartInfo("git")
            {
                Arguments = $"push --set-upstream origin feature/cake-{settings.Version}",
                WorkingDirectory = path.FullPath
            };

            var process = Process.Start(info);
            if (process == null)
            {
                return false;
            }

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                _log.Error("Something went wrong when pushing to Git.");
                return false;
            }

            return true;
        }

        private void CreatePullRequest(GitHubClient client, UpdateSettings settings, (string owner, string repository) info)
        {
            _log.Write("Creating pull request...");
            client.Repository.PullRequest.Create(
                info.owner,
                info.repository,
                new NewPullRequest($"Update to Cake {settings.Version}.", $"{settings.User}:feature/cake-{settings.Version}", "develop")
                {
                    Body = $"This PR was automatically generated by a tool (not @{settings.User}).\n\n:warning: DO NOT merge this without review! :smile:"
                }).Wait();
        }
    }
}
