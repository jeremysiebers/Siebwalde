// SiebwaldeApp.Tests/Infrastructure/TestLogFactory.cs
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace SiebwaldeApp.Core
{
    // Minimal stub; pas aan als jouw echte ILogFactory extra members heeft
    public sealed class TestLogFactory : ILogFactory
    {
        // optioneel: in-memory buffer om asserts op te doen
        public ConcurrentBag<string> Lines { get; } = new();
        public LogOutputLevel LogOutputLevel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IncludeLogOriginDetails { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event Action<(string Message, LogLevel Level, string loggerinstance)> NewLog;

        public void Log(string message, string instance)
        {
            Lines.Add($"[{instance}] {message}");
            // Console.WriteLine(...) mag ook als je het wil zien tijdens test runs
        }

        public void CreateLogger(string instance)
        {
            // no-op in tests
        }

        public ILogFactory GetLogger(string path, string instance)
        {
            // In je productiecode return je vaak een "child" logger;
            // voor tests is "this" voldoende.
            return this;
        }

        public void AddLogger(ILogger logger)
        {
            // no-op in tests
        }

        public void RemoveLogger(ILogger logger)
        {
            // no-op in tests
        }

        public void Log(string message, string loggerinstance, LogLevel level = LogLevel.Informative, [CallerMemberName] string origin = "", [CallerFilePath] string filepath = "", [CallerLineNumber] int linenumber = 0)
        {
            // no-op in tests
        }
    }
}
