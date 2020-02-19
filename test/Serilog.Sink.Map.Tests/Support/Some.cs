using System;
using System.Collections.Generic;
using Serilog.Events;
using Xunit.Sdk;

namespace Serilog.Sinks.Map.Tests.Support
{
    static class Some
    {
        public static LogEvent LogEvent(string messageTemplate, params object[] propertyValues)
        {
            return LogEvent(null, messageTemplate, propertyValues);
        }

        public static LogEvent LogEvent(Exception exception, string messageTemplate, params object[] propertyValues)
        {
            return LogEvent(LogEventLevel.Information, exception, messageTemplate, propertyValues);
        }

        public static LogEvent LogEvent(LogEventLevel level, Exception exception, string messageTemplate, params object[] propertyValues)
        {
            var log = new LoggerConfiguration().CreateLogger();
#pragma warning disable Serilog004 // Constant MessageTemplate verifier
            if (!log.BindMessageTemplate(messageTemplate, propertyValues, out var template, out var properties))
#pragma warning restore Serilog004 // Constant MessageTemplate verifier
            {
                throw new XunitException("Template could not be bound.");
            }
            return new LogEvent(DateTimeOffset.Now, level, exception, template, properties);
        }
    }
}
