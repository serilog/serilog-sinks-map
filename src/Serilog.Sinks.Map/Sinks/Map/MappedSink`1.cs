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
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.Map
{
    class MappedSink<TKey> : ILogEventSink, IDisposable
    {
        readonly Func<LogEvent, TKey> _keySelector;
        readonly Action<TKey, LoggerSinkConfiguration> _configure;
        readonly int? _sinkMapCountLimit;
        readonly object _sync = new object();
        readonly Dictionary<KeyValuePair<TKey, bool>, Logger> _sinkMap = new Dictionary<KeyValuePair<TKey, bool>, Logger>();
        bool _disposed;

        public MappedSink(Func<LogEvent, TKey> keySelector,
                          Action<TKey, LoggerSinkConfiguration> configure,
                          int? sinkMapCountLimit)
        {
            _keySelector = keySelector;
            _configure = configure;
            _sinkMapCountLimit = sinkMapCountLimit;
        }

        public void Emit(LogEvent logEvent)
        {
            var key = new KeyValuePair<TKey, bool>(_keySelector(logEvent), false);

            lock (_sync)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(MappedSink<TKey>), "The mapped sink has been disposed.");

                if (_sinkMap.TryGetValue(key, out var existing))
                {
                    existing.Write(logEvent);
                    return;
                }

                var config = new LoggerConfiguration()
                    .MinimumLevel.Is(LevelAlias.Minimum);

                _configure(key.Key, config.WriteTo);
                var sink = config.CreateLogger();

                if (_sinkMapCountLimit == 0)
                {
                    using (sink)
                        sink.Write(logEvent);
                }
                else if (_sinkMapCountLimit == null || _sinkMapCountLimit > _sinkMap.Count)
                {
                    // This case is a little faster as no EH nor iteration is required
                    _sinkMap[key] = sink;
                    sink.Write(logEvent);
                }
                else
                {
                    _sinkMap[key] = sink;
                    try
                    {
                        sink.Write(logEvent);
                    }
                    finally
                    {
                        while(_sinkMap.Count > _sinkMapCountLimit.Value)
                        {
                            foreach (var k in _sinkMap.Keys)
                            {
                                if (key.Equals(k))
                                    continue;

                                var removed = _sinkMap[k];
                                _sinkMap.Remove(k);
                                removed.Dispose();
                                break;
                            }
                        }                        
                    }
                }
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;

                _disposed = true;

                var values = _sinkMap.Values;
                _sinkMap.Clear();
                foreach (var sink in values)
                {
                    sink.Dispose();
                }
            }
        }
    }
}
