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
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Map;

namespace Serilog
{
    /// <summary>
    /// Extends Serilog configuration with methods for selecting sink instances base on a log event property.
    /// </summary>
    public static class MapLoggerConfigurationExtensions
    {
        /// <summary>
        /// Dispatch log events to a set of sinks keyed on a log event property.
        /// </summary>
        /// <param name="loggerSinkConfiguration">The logger sink configuration.</param>
        /// <param name="keyPropertyName">The name of a scalar-valued property to use as a sink selector.</param>
        /// <param name="configure">An action to configure the target sink given a key property value.</param>
        /// <param name="defaultKey">The key property value to use when no appropriate value is attached to the log event.</param>
        /// <param name="sinkMapCountLimit">Limits the number of sinks that will be held open concurrently within the map.
        /// The default is to let the map grow unbounded; smaller numbers will cause sinks to be evicted when the limit is
        /// exceeded. To keep no sinks open, zero may be specified.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required 
        /// in order to write an event to the sink.</param>
        /// <param name="levelSwitch">A level switch to dynamically select the minimum level for events passed to the  sink.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration Map(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string keyPropertyName,
            Action<string, LoggerSinkConfiguration> configure,
            string defaultKey = null,
            int? sinkMapCountLimit = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch levelSwitch = null)
        {
            return Map<string>(loggerSinkConfiguration, keyPropertyName, configure, defaultKey,
                               sinkMapCountLimit, restrictedToMinimumLevel, levelSwitch);
        }

        /// <summary>
        /// Dispatch log events to a set of sinks keyed on a log event property.
        /// </summary>
        /// <param name="loggerSinkConfiguration">The logger sink configuration.</param>
        /// <param name="keyPropertyName">The name of a scalar-valued property to use as a sink selector.</param>
        /// <param name="configure">An action to configure the target sink given a key property value.</param>
        /// <param name="defaultKey">The key property value to use when no appropriate value is attached to the log event.</param>
        /// <param name="sinkMapCountLimit">Limits the number of sinks that will be held open concurrently within the map.
        /// The default is to let the map grow unbounded; smaller numbers will cause sinks to be evicted when the limit is
        /// exceeded. To keep no sinks open, zero may be specified.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required 
        /// in order to write an event to the sink.</param>
        /// <param name="levelSwitch">A level switch to dynamically select the minimum level for events passed to the  sink.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration Map<TKey>(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string keyPropertyName,
            Action<TKey, LoggerSinkConfiguration> configure,
            TKey defaultKey = default(TKey),
            int? sinkMapCountLimit = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch levelSwitch = null)
        {
            if (loggerSinkConfiguration == null) throw new ArgumentNullException(nameof(loggerSinkConfiguration));
            if (keyPropertyName == null) throw new ArgumentNullException(nameof(keyPropertyName));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            return Map(loggerSinkConfiguration, le =>
            {
                if (le.Properties.TryGetValue(keyPropertyName, out var v) &&
                    v is ScalarValue sv &&
                    sv.Value is TKey key)
                {
                    return key;
                }

                return defaultKey;
            }, configure, sinkMapCountLimit, restrictedToMinimumLevel, levelSwitch);
        }

        /// <summary>
        /// Dispatch log events to a set of sinks keyed on a log event property.
        /// </summary>
        /// <param name="loggerSinkConfiguration">The logger sink configuration.</param>
        /// <param name="configure">An action to configure the target sink given a key property value.</param>
        /// <param name="keySelector">A function to select a key value given a log event.</param>
        /// <param name="sinkMapCountLimit">Limits the number of sinks that will be held open concurrently within the map.
        /// The default is to let the map grow unbounded; smaller numbers will cause sinks to be evicted when the limit is
        /// exceeded. To keep no sinks open, zero may be specified.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required 
        /// in order to write an event to the sink.</param>
        /// <param name="levelSwitch">A level switch to dynamically select the minimum level for events passed to the  sink.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration Map<TKey>(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            Func<LogEvent, TKey> keySelector,
            Action<TKey, LoggerSinkConfiguration> configure,
            int? sinkMapCountLimit = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch levelSwitch = null)
        {
            if (loggerSinkConfiguration == null) throw new ArgumentNullException(nameof(loggerSinkConfiguration));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            if (sinkMapCountLimit.HasValue && sinkMapCountLimit.Value < 0) throw new ArgumentOutOfRangeException(nameof(sinkMapCountLimit));

            return loggerSinkConfiguration.Sink(new MappedSink<TKey>(keySelector, configure, sinkMapCountLimit),
                                                restrictedToMinimumLevel, levelSwitch);
        }
    }
}
