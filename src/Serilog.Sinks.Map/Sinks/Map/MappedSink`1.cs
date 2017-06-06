// Serilog.Sinks.Seq Copyright 2017 Serilog Contributors
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
        readonly object _sync = new object();
        readonly Dictionary<TKey, Logger> _sinkMap = new Dictionary<TKey, Logger>();

        public MappedSink(Func<LogEvent, TKey> keySelector, Action<TKey, LoggerSinkConfiguration> configure)
        {
            _keySelector = keySelector;
            _configure = configure;
        }

        public void Emit(LogEvent logEvent)
        {
            var key = _keySelector(logEvent);

            Logger sink;
            lock (_sync)
            {
                if (!_sinkMap.TryGetValue(key, out sink))
                {
                    var config = new LoggerConfiguration()
                        .MinimumLevel.Is(LevelAlias.Minimum);

                    _configure(key, config.WriteTo);
                    sink = _sinkMap[key] = config.CreateLogger();
                }
            }

            // Outside the lock to improve concurrency; this means the sink
            // may throw ObjectDisposedException, which is fine.
            sink.Write(logEvent);
        }

        public void Dispose()
        {
            lock (_sync)
            {
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
