using System;

using Xunit;

namespace Serilog.Sinks.Rollbar.Tests
{
    public class RollbarSinkTests
    {
        public RollbarSinkTests()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Rollbar(Environment.GetEnvironmentVariable("rollbar:token"), transform: payload => { payload.Data.Custom.Add("k", "Additional info"); })
                .CreateLogger();
        }

        [Fact]
        public void LogIExceptionMessage()
        {
            try
            {
                throw new NullReferenceException();
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Exception message {DateTime}", DateTime.UtcNow);
            }
        }
    }
}
