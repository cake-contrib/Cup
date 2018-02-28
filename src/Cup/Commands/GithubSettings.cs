using Spectre.CommandLine;

namespace Cup.Commands
{
    public class GithubSettings
    {
        [CommandArgument(0, "<USER>")]
        public string User { get; set; }

        [CommandArgument(1, "<TOKEN>")]
        public string Token { get; set; }
    }
}
