using System;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.Map.Tests.Support
{
    class DisposeTrackingSink : ILogEventSink, IDisposable
    {
        public void Emit(LogEvent logEvent)
        {
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public bool IsDisposed { get; private set; }
    }
}
