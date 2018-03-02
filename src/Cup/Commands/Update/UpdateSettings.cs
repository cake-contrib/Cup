using System.ComponentModel;
using Cup.Infrastructure.Converters;
using JetBrains.Annotations;
using Spectre.CommandLine;
using Spectre.System.IO;

namespace Cup.Commands.Update
{
    public class UpdateSettings : GithubSettings
    {
        [CommandArgument(0, "<REPOSITORY>")]
        [Description("The repository to update (owner/repository).")]
        public string Repository { get; [UsedImplicitly] set; }

        [CommandArgument(1, "<VERSION>")]
        [Description("The Cake version to update to.")]
        public string Version { get; [UsedImplicitly] set; }

        [CommandOption("--working <PATH>")]
        [TypeConverter(typeof(DirectoryPathConverter))]
        [Description("The working directory to use.")]
        public DirectoryPath WorkingDirectory { get; [UsedImplicitly] set; }

        [CommandOption("--commit")]
        [Description("Determines wheter or not to create a commit.")]
        public bool Commit { get; [UsedImplicitly] set; }

        [CommandOption("--push")]
        [Description("Determines wheter or not to push the commit. Requires the --commit parameter.")]
        public bool Push { get; [UsedImplicitly] set; }

        [CommandOption("--pr")]
        [Description("Determines wheter or not to create a PR. Requires the --push parameter.")]
        public bool OpenPullRequest { get; [UsedImplicitly] set; }
    }
}
