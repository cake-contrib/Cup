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
        public string Repository { get; [UsedImplicitly] set; }

        [CommandArgument(0, "<VERSION>")]
        public string Version { get; [UsedImplicitly] set; }

        [CommandOption("-w|--working <PATH>")]
        [TypeConverter(typeof(DirectoryPathConverter))]
        public DirectoryPath WorkingDirectory { get; [UsedImplicitly] set; }

        [CommandOption("-c|--commit")]
        public bool Commit { get; [UsedImplicitly] set; }

        [CommandOption("-p|--push")]
        public bool Push { get; [UsedImplicitly] set; }

        [CommandOption("--pr")]
        public bool OpenPullRequest { get; [UsedImplicitly] set; }
    }
}
