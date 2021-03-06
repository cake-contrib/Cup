using System.ComponentModel;
using JetBrains.Annotations;
using Spectre.CommandLine;

namespace Cup.Commands
{
    public class GithubSettings
    {
        [CommandArgument(0, "<USER>")]
        [Description("The GitHub username to use.")]
        public string User { get; [UsedImplicitly] set; }

        [CommandArgument(1, "<TOKEN>")]
        [Description("The GitHub access token to use for the provided user.")]
        public string Token { get; [UsedImplicitly] set; }
    }
}
