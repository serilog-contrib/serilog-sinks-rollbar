using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Rollbar;
using Rollbar.DTOs;

using Serilog.Core;
using Serilog.Events;

namespace Serilog
{
    /// <inheritdoc />
    public class RollbarSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;

        private readonly bool _asyncLogging;

        private readonly TimeSpan _asyncLoggingTimeout;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RollbarSink" /> class.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="proxyAddress">The proxy address.</param>
        /// <param name="scrubFields">The scrub fields.</param>
        /// <param name="asyncLogging">If set to <c>true</c> [asynchronous logging].</param>
        /// <param name="asyncLoggingTimeout">
        ///     The asynchronous logging timeout, the parameter is ignored when the logging is async.
        ///     By default 30 seconds.
        /// </param>
        /// <exception cref="ArgumentException">Value cannot be null or empty. - accessToken</exception>
        public RollbarSink(
            string accessToken,
            string environment,
            IFormatProvider formatProvider,
            Action<Payload> transform,
            string proxyAddress,
            string[] scrubFields,
            bool asyncLogging,
            TimeSpan asyncLoggingTimeout)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(accessToken));
            }

            _formatProvider = formatProvider;
            _asyncLogging = asyncLogging;
            _asyncLoggingTimeout = asyncLoggingTimeout;

            var serverConfig = new Server
                                   {
                                       { Server.ReservedProperties.Host, Environment.MachineName },
                                       { Server.ReservedProperties.CodeVersion, Assembly.GetEntryAssembly()?.GetName().Version?.ToString() }
                                   };
            var rollbarConfig = new RollbarConfig(accessToken) { Transform = transform, ProxyAddress = proxyAddress, Server = serverConfig };

            if (!string.IsNullOrWhiteSpace(environment))
            {
                rollbarConfig.Environment = environment;
            }

            if (scrubFields != null && scrubFields.Length > 0)
            {
                rollbarConfig.ScrubFields = scrubFields;
            }

            if (_asyncLoggingTimeout == default(TimeSpan))
            {
                // ReSharper disable once ExceptionNotDocumented
                _asyncLoggingTimeout = TimeSpan.FromSeconds(30);
            }

            RollbarLocator.RollbarInstance.Configure(rollbarConfig);
        }

        /// <inheritdoc />
        public void Emit(LogEvent logEvent)
        {
            Func<ErrorLevel, object, IDictionary<string, object>, object> logMessageAction;
            if (_asyncLogging)
            {
                logMessageAction = RollbarLocator.RollbarInstance.Log;
            }
            else
            {
                logMessageAction = RollbarLocator.RollbarInstance.AsBlockingLogger(_asyncLoggingTimeout)
                    .Log;
            }

            var rollbarLevel = ToRollbarLevel(logEvent.Level);

            Log(rollbarLevel, logMessageAction, logEvent);
        }

        private void Log(
            ErrorLevel level,
            Func<ErrorLevel, object, IDictionary<string, object>, object> logAction,
            LogEvent logEvent)
        {
            IDictionary<string, object> properties = null;
            if (logEvent.Properties?.Any() == true)
            {
                properties = new Dictionary<string, object>();
                foreach (var pair in logEvent.Properties)
                {
                    properties.Add(pair.Key, pair.Value.ToString());
                }
            }

            var message = logEvent.RenderMessage(_formatProvider);
            if (logEvent.Exception == null)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    logAction(level, message, properties);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(message))
                {
                    if (properties == null)
                    {
                        properties = new Dictionary<string, object>();
                    }

                    const string MessageKey = "message";
                    properties.Add(
                        !properties.ContainsKey(MessageKey)
                            ? MessageKey
                            : Guid.NewGuid()
                                .ToString(),
                        message);
                }

                logAction(level, logEvent.Exception, properties);
            }
        }

        private ErrorLevel ToRollbarLevel(LogEventLevel level)
        {
            switch (level)
            {
                case LogEventLevel.Verbose:
                case LogEventLevel.Debug:
                    return ErrorLevel.Debug;
                case LogEventLevel.Information:
                    return ErrorLevel.Info;
                case LogEventLevel.Warning:
                    return ErrorLevel.Warning;
                case LogEventLevel.Error:
                    return ErrorLevel.Error;
                case LogEventLevel.Fatal:
                    return ErrorLevel.Critical;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }
    }
}
