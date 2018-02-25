using System;

using Rollbar.DTOs;

using Serilog.Configuration;
using Serilog.Events;

namespace Serilog
{
    /// <summary>
    ///     Contains extension methods for Serilog configuration.
    /// </summary>
    public static class RollbarSinkExtensions
    {
        /// <summary>
        ///     Sentries the specified access token.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="restrictedToMinimumLevel">The restricted to minimum level.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="proxyAddress">The proxy address.</param>
        /// <param name="scrubFields">The scrub fields.</param>
        /// <param name="asyncLogging">If set to <c>true</c> the logging will be async.</param>
        /// <param name="asyncLoggingTimeout">
        ///     The asynchronous logging timeout, the parameter ignored when logging is sync. By
        ///     default 30 seconds.
        /// </param>
        /// <returns>
        ///     The logger configuration.
        /// </returns>
        // ReSharper disable once StyleCop.SA1625
        public static LoggerConfiguration Rollbar(
            this LoggerSinkConfiguration loggerConfiguration,
            string accessToken,
            string environment = "production",
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Error,
            IFormatProvider formatProvider = null,
            Action<Payload> transform = null,
            string proxyAddress = null,
            string[] scrubFields = null,
            bool asyncLogging = false,
            TimeSpan asyncLoggingTimeout = default(TimeSpan))
        {
            return loggerConfiguration.Sink(
                new RollbarSink(
                    accessToken,
                    environment,
                    formatProvider,
                    transform,
                    proxyAddress,
                    scrubFields,
                    asyncLogging,
                    asyncLoggingTimeout),
                restrictedToMinimumLevel);
        }
    }
}
