using System;
using JetBrains.Annotations;

namespace Cup.Diagnostics
{
    [UsedImplicitly]
    public sealed class ConsoleLog : IConsoleLog
    {
        private int _indentation;

        private sealed class ConsoleLogScope : IConsoleLog
        {
            private readonly ConsoleLog _log;

            public ConsoleLogScope(ConsoleLog log)
            {
                _log = log;
                _log._indentation++;
            }

            public void Dispose()
            {
                _log._indentation--;
            }

            public IConsoleLog Indent()
            {
                return new ConsoleLogScope(_log);
            }

            public void Write(string format, params object[] args)
            {
                _log.Write(format, args);
            }

            public void Error(string format, params object[] args)
            {
                _log.Error(format, args);
            }
        }

        public IConsoleLog Indent()
        {
            return new ConsoleLogScope(this);
        }

        public void Write(string format, params object[] args)
        {
            Console.ForegroundColor = _indentation == 0 ? ConsoleColor.Yellow : ConsoleColor.Gray;
            Console.WriteLine("{0}{1}", new string(' ', _indentation * 2), string.Format(format, args));
            Console.ResetColor();
        }

        public void Error(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("{0}{1}", new string(' ', _indentation * 2), string.Format(format, args));
            Console.ResetColor();
        }

        public void Dispose()
        {
        }
    }
}
