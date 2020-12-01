﻿using System.Collections.Generic;
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

        [Fact]
        public void WithNoMapSizeLimitSinksAreNotDisposed()
        {
            var a = Some.LogEvent("Hello, {Name}!", "Alice");

            var sink = new DisposeTrackingSink();
            var log = new LoggerConfiguration()
                .WriteTo.Map("Name", (name, wt) => wt.Sink(sink))
                .CreateLogger();

            log.Write(a);

            Assert.False(sink.IsDisposed);
        }

        [Fact]
        public void WithMapSizeZeroSinksAreImmediatelyDisposed()
        {
            var a = Some.LogEvent("Hello, {Name}!", "Alice");

            var sink = new DisposeTrackingSink();
            var log = new LoggerConfiguration()
                .WriteTo.Map("Name", (name, wt) => wt.Sink(sink), sinkMapCountLimit: 0)
                .CreateLogger();

            log.Write(a);

            Assert.True(sink.IsDisposed);
        }

        [Fact]
        public void WhenSinkMapOverflowsUnmappedSinksAreDisposed()
        {
            var a = Some.LogEvent("Hello, {Name}!", "Alice");
            var b = Some.LogEvent("Hello, {Name}!", "Bob");

            var sinkA = new DisposeTrackingSink();
            var sinkB = new DisposeTrackingSink();
            var log = new LoggerConfiguration()
                .WriteTo.Map("Name", (name, wt) => wt.Sink(name == "Alice" ? sinkA : sinkB), sinkMapCountLimit: 1)
                .CreateLogger();

            log.Write(a);
            log.Write(b);

            Assert.True(sinkA.IsDisposed);
            Assert.False(sinkB.IsDisposed);
        }

        [Fact]
        public void NullReferenceTypeKeysAreSupported()
        {
            var a = Some.LogEvent("Hello, {Name}!", new object[] { null });

            var received = new List<(string, LogEvent)>();

            var log = new LoggerConfiguration()
                .WriteTo.Map("Name", (name, wt) => wt.Sink(new DelegatingSink(e => received.Add((name, e)))))
                .CreateLogger();

            log.Write(a);

            Assert.Single(received);
            Assert.Null(received[0].Item1);
        }

        [Fact]
        public void NonGenericVersionShouldNotCheckKeyType()
        {
            var a = Some.LogEvent("Hello, {Name}!", 123_456);

            var received = new List<(string, LogEvent)>();

            var log = new LoggerConfiguration()
                .WriteTo.Map("Name", (name, wt) => wt.Sink(new DelegatingSink(e => received.Add((name, e)))))
                .CreateLogger();

            log.Write(a);

            Assert.Single(received);
            Assert.Equal("123456", received[0].Item1);
        }

        [Fact]
        public void GenericVersionShouldCheckKeyType()
        {
            var a = Some.LogEvent("Hello, {Name}!", 123_456);
            var b = Some.LogEvent("Hello, {Name}!", "Alice");

            var received = new List<(string, LogEvent)>();

            var log = new LoggerConfiguration()
                .WriteTo.Map<string>("Name", (name, wt) => wt.Sink(new DelegatingSink(e => received.Add((name, e)))))
                .CreateLogger();

            log.Write(a);
            log.Write(b);

            Assert.Single(received);
            Assert.Equal("Alice", received[0].Item1);
        }

        [Fact]
        public void ShouldSkipEmitWhenNoAppropriateValueIsAttachedToLogEvent()
        {
            var a = Some.LogEvent("Hello, World!");

            var calls = 0;

            var log = new LoggerConfiguration()
                .WriteTo.Map("Name", (_, __) => ++calls)
                .CreateLogger();

            log.Write(a);

            Assert.Equal(0, calls);
        }

        [Fact]
        public void DefaultKeyIsUsedWhenNoAppropriateValueIsAttachedToLogEvent()
        {
            var a = Some.LogEvent("Hello, World!");

            var received = new List<(string, LogEvent)>();

            var log = new LoggerConfiguration()
                .WriteTo.Map("Name", "anonymous", (name, wt) => wt.Sink(new DelegatingSink(e => received.Add((name, e)))))
                .CreateLogger();

            log.Write(a);

            Assert.Single(received);
            Assert.Equal("anonymous", received[0].Item1);
        }

        [Fact]
        public void DefaultKeyIsUsedWhenNullValueIsAttachedToLogEvent()
        {
            var a = Some.LogEvent("Hello, {Name}!", (object)null);

            var received = new List<(string, LogEvent)>();

            var log = new LoggerConfiguration()
                .WriteTo.Map("Name", "anonymous", (name, wt) => wt.Sink(new DelegatingSink(e => received.Add((name, e)))))
                .CreateLogger();

            log.Write(a);

            Assert.Single(received);
            Assert.Equal("anonymous", received[0].Item1);
        }

        [Fact]
        public void DefaultKeyIsUsedWhenGenericNullValueIsAttachedToLogEvent()
        {
            var a = Some.LogEvent("Hello, {Name}!", (object)null);

            var received = new List<(string, LogEvent)>();

            var log = new LoggerConfiguration()
                .WriteTo.Map<string>("Name", "anonymous", (name, wt) => wt.Sink(new DelegatingSink(e => received.Add((name, e)))))
                .CreateLogger();

            log.Write(a);

            Assert.Single(received);
            Assert.Equal("anonymous", received[0].Item1);
        }
        
        [Fact]
        public void SinksAreDisposedWithMapSinkDispose()
        {
            var a = Some.LogEvent("Hello, {Name}!", "Alice");

            var sink = new DisposeTrackingSink();
            using (var log = new LoggerConfiguration()
                .WriteTo.Map("Name", (name, wt) => wt.Sink(sink))
                .CreateLogger())
            {
                log.Write(a);
            }

            Assert.True(sink.IsDisposed);
        }
    }
}
