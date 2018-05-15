using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Rollbar;
using Rollbar.DTOs;

using Serilog.Core;
using Serilog.Events;

using Exception = System.Exception;

namespace Serilog
{
    /// <inheritdoc />
    public class RollbarSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;

        private readonly Rollbar.ILogger _rollbar;

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

            var rollbarConfig = new RollbarConfig(accessToken) { Transform = transform, ProxyAddress = proxyAddress };

            rollbarConfig.Server = new Server
            {
                {Server.ReservedProperties.Host, Environment.MachineName},
                {Server.ReservedProperties.CodeVersion, Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString()}
            };

            if (!string.IsNullOrWhiteSpace(environment))
            {
                rollbarConfig.Environment = environment;
            }

            if (scrubFields != null && scrubFields.Length > 0)
            {
                rollbarConfig.ScrubFields = scrubFields;
            }

            if (asyncLoggingTimeout == default(TimeSpan))
            {
                // ReSharper disable once ExceptionNotDocumented
                asyncLoggingTimeout = TimeSpan.FromSeconds(30);
            }

            var rollbar = RollbarFactory.CreateNew()
                .Configure(rollbarConfig);
            _rollbar = asyncLogging ? rollbar : rollbar.AsBlockingLogger(asyncLoggingTimeout);
        }

        /// <inheritdoc />
        public void Emit(LogEvent logEvent)
        {
            switch (logEvent.Level)
            {
                case LogEventLevel.Verbose:
                case LogEventLevel.Debug:
                    Log(_rollbar.Debug, _rollbar.Debug, logEvent);
                    break;
                case LogEventLevel.Information:
                    Log(_rollbar.Info, _rollbar.Info, logEvent);
                    break;
                case LogEventLevel.Warning:
                    Log(_rollbar.Warning, _rollbar.Warning, logEvent);
                    break;
                case LogEventLevel.Error:
                    Log(_rollbar.Error, _rollbar.Error, logEvent);
                    break;
                case LogEventLevel.Fatal:
                    Log(_rollbar.Critical, _rollbar.Critical, logEvent);
                    break;
            }
        }

        private void Log(
            Func<string, IDictionary<string, object>, Rollbar.ILogger> logMessage,
            Func<Exception, IDictionary<string, object>, Rollbar.ILogger> logException,
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
                logMessage(message, properties);
            }
            else
            {
                if (properties != null)
                {
                    const string MessageKey = "message";
                    properties.Add(
                        !properties.ContainsKey(MessageKey)
                            ? MessageKey
                            : Guid.NewGuid()
                                .ToString(),
                        message);
                }

                logException(logEvent.Exception, properties);
            }
        }
    }
}
