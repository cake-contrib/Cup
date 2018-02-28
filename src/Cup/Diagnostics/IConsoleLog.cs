using System;

namespace Cup.Diagnostics
{
    public interface IConsoleLog : IDisposable
    {
        IConsoleLog Indent();
        void Write(string format, params object[] args);
        void Error(string format, params object[] args);
    }
}