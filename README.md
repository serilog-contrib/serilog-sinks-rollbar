# Serilog.Sinks.Rollbar

[![Build status](https://ci.appveyor.com/api/projects/status/6d4db0c1uapprb5h?svg=true)](https://ci.appveyor.com/project/olsh/serilog-sinks-rollbar)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=serilog-sinks-rollbar&metric=alert_status)](https://sonarcloud.io/dashboard?id=serilog-sinks-rollbar)
[![NuGet](https://img.shields.io/nuget/v/Serilog.Sinks.Rollbar.svg)](https://www.nuget.org/packages/Serilog.Sinks.Rollbar/)

A Serilog sink which writes events to [Rollbar](https://rollbar.com/).

## Installation

The library is available as a [Nuget package](https://www.nuget.org/packages/Serilog.Sinks.Rollbar/).
```
Install-Package Serilog.Sinks.Rollbar
```

## Get started

```csharp
var log = new LoggerConfiguration()
    .WriteTo.Rollbar("Post server access token")
    .Enrich.FromLogContext()
    .CreateLogger();

// By default, only messages with level errors and higher are captured
log.Error("This error goes to Rollbar.");
```
