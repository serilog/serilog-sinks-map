// Serilog.Sinks.Map Copyright 2017 Serilog Contributors
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.Map
{
    /// <summary>
    /// A function delegate to select a key value given a log event, if exists.
    /// </summary>
    /// <typeparam name="TKey">The type of the key value.</typeparam>
    /// <param name="logEvent">The log event.</param>
    /// <param name="key">The selected key.</param>
    /// <returns>true, if a key can be selected, or false, otherwise.</returns>
    public delegate bool KeySelector<TKey>(LogEvent logEvent, out TKey key);

    class MappedSink<TKey> : ILogEventSink, IDisposable
    {
        readonly KeySelector<TKey> _keySelector;
        readonly Action<TKey, LoggerSinkConfiguration> _configure;
        readonly int? _sinkMapCountLimit;
        readonly object _sync = new object();
        readonly Dictionary<KeyValuePair<TKey, bool>, ILogEventSink> _sinkMap = new Dictionary<KeyValuePair<TKey, bool>, ILogEventSink>();
        bool _disposed;

        // ReSharper disable once StaticMemberInGenericType
        static readonly LoggerSinkConfiguration LoggerSinkConfiguration = new LoggerConfiguration().WriteTo;

        public MappedSink(KeySelector<TKey> keySelector,
                          Action<TKey, LoggerSinkConfiguration> configure,
                          int? sinkMapCountLimit)
        {
            _keySelector = keySelector;
            _configure = configure;
            _sinkMapCountLimit = sinkMapCountLimit;
        }

        // All writes are synchronized, even though this could be avoided in a few cases; the reasoning behind this is that
        // changes in synchronization behavior between writes and with different map count limits could lead to surprises
        // when using the sink with log files, which are one of the main use cases. Since most sinks already synchronize
        // writes or offload work to background threads, this is a reasonable trade-off.
        public void Emit(LogEvent logEvent)
        {
            if (!_keySelector(logEvent, out var keyValue))
                return;

            var key = new KeyValuePair<TKey, bool>(keyValue, false);

            lock (_sync)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(MappedSink<TKey>), "The mapped sink has been disposed.");

                if (_sinkMap.TryGetValue(key, out var existing))
                {
                    existing.Emit(logEvent);
                    return;
                }

                var sink = CreateSink(key.Key);

                if (_sinkMapCountLimit == 0)
                {
                    using (sink as IDisposable)
                        sink.Emit(logEvent);
                }
                else if (_sinkMapCountLimit == null || _sinkMapCountLimit > _sinkMap.Count)
                {
                    // This case is a little faster as no EH nor iteration is required
                    _sinkMap[key] = sink;
                    sink.Emit(logEvent);
                }
                else
                {
                    _sinkMap[key] = sink;
                    try
                    {
                        sink.Emit(logEvent);
                    }
                    finally
                    {
                        while (_sinkMap.Count > _sinkMapCountLimit.Value)
                        {
                            foreach (var k in _sinkMap.Keys)
                            {
                                if (key.Equals(k))
                                    continue;

                                var removed = _sinkMap[k];
                                _sinkMap.Remove(k);
                                (removed as IDisposable)?.Dispose();
                                break;
                            }
                        }
                    }
                }
            }
        }

        ILogEventSink CreateSink(TKey key)
        {
            // Allocates a few delegates, but avoids a lot more allocation in the `LoggerConfiguration`/`Logger` machinery.
            ILogEventSink sink = null;
            LoggerSinkConfiguration.Wrap(
                LoggerSinkConfiguration,
                s => sink = s,
                config => _configure(key, config),
                LevelAlias.Minimum,
                null);

            return sink;
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;

                _disposed = true;

                var values = _sinkMap.Values.ToArray();
                _sinkMap.Clear();
                foreach (var sink in values)
                {
                    (sink as IDisposable)?.Dispose();
                }
            }
        }
    }
}
