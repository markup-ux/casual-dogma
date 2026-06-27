using System;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.Cli.Command
{
    public class ErrorCommand : ICommand
    {
        private static readonly ILogger Logger = LogProvider.Logger(typeof(ErrorCommand));

        private readonly ErrorMonitor _errorMonitor;

        public ErrorCommand(ErrorMonitor errorMonitor)
        {
            _errorMonitor = errorMonitor;
        }

        public string Key => "errors";

        public string Description =>
            $"Show errors detected by the server.{Environment.NewLine}" +
            $"errors          - aggregated error report{Environment.NewLine}" +
            $"errors recent   - the most recent errors{Environment.NewLine}" +
            $"errors clear    - reset the error counters";

        public CommandResultType Run(CommandParameter parameter)
        {
            if (parameter.Arguments.Contains("clear"))
            {
                _errorMonitor.Reset();
                Logger.Info("Error counters reset.");
                return CommandResultType.Completed;
            }

            if (parameter.Arguments.Contains("recent"))
            {
                Logger.Info(_errorMonitor.BuildRecent());
                return CommandResultType.Completed;
            }

            Logger.Info(_errorMonitor.BuildReport());
            return CommandResultType.Completed;
        }

        public void Shutdown()
        {
        }
    }
}
