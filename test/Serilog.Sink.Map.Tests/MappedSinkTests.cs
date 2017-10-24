using System.Collections.Generic;
using Serilog.Events;
using Serilog.Sinks.Map.Tests.Support;
using Xunit;

namespace Serilog.Sinks.Map.Tests
{
    public class MappedSinkTests
    {
        [Fact]
        public void EventsReachSinksSelectedByTheConfiguredProperty()
        {
            var a = Some.LogEvent("Hello, {Name}!", "Alice");
            var b = Some.LogEvent("Hello, {Name}!", "Bob");

            var received = new List<(string, LogEvent)>();

            var log = new LoggerConfiguration()
                .WriteTo.Map("Name", (name, wt) => wt.Sink(new DelegatingSink(e => received.Add((name, e)))))
                .CreateLogger();

            log.Write(a);
            log.Write(b);

            Assert.Equal(2, received.Count);
            Assert.Equal("Alice", received[0].Item1);
            Assert.Equal("Bob", received[1].Item1);
        }

        [Fact]
        public void WithUnlimitedMapSizeSinksAreRetained()
        {
            var a = Some.LogEvent("Hello, {Name}!", "Alice");
            var calls = 0;

            var log = new LoggerConfiguration()
                .WriteTo.Map("Name", (name, wt) =>
                {
                    ++calls;
                })
                .CreateLogger();

            log.Write(a);
            log.Write(a);

            Assert.Equal(1, calls);
        }

        [Fact]
        public void WithMapSize1LastSinksIsRetained()
        {
            var a = Some.LogEvent("Hello, {Name}!", "Alice");
            var calls = 0;

            var log = new LoggerConfiguration()
                .WriteTo.Map("Name", (name, wt) =>
                {
                    ++calls;
                }, sinkMapCountLimit: 1)
                .CreateLogger();

            log.Write(a);
            log.Write(a);

            Assert.Equal(1, calls);
        }

        [Fact]
        public void WithMapSizeNLastNSinksAreRetained()
        {
            var a = Some.LogEvent("Hello, {Name}!", "Alice");
            var b = Some.LogEvent("Hello, {Name}!", "Bob");
            var calls = 0;

            var log = new LoggerConfiguration()
                .WriteTo.Map("Name", (name, wt) =>
                {
                    ++calls;
                }, sinkMapCountLimit: 1)
                .CreateLogger();

            log.Write(a);
            log.Write(b);
            log.Write(a);

            Assert.Equal(3, calls);
        }

        [Fact]
        public void WithZeroMapSizeSinksAreRecycledImmediately()
        {
            var a = Some.LogEvent("Hello, {Name}!", "Alice");
            var calls = 0;

            var log = new LoggerConfiguration()
                .WriteTo.Map("Name", (name, wt) =>
                {
                    ++calls;
                }, sinkMapCountLimit: 0)
                .CreateLogger();

            log.Write(a);
            log.Write(a);

            Assert.Equal(2, calls);
        }
    }
}
