# Serilog.Sinks.Map [![Build status](https://ci.appveyor.com/api/projects/status/m7svuyb4bkve3q6y?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-map) [![NuGet Pre Release](https://img.shields.io/nuget/vpre/Serilog.Sinks.Map.svg)](https://www.nuget.org/packages/serilog.sinks.map)

A Serilog sink wrapper that dispatches events based on a property value.

### Getting started

Install the package from NuGet:

```powershell
dotnet add package Serilog.Sinks.Map
```

The `WriteTo.Map()` method accepts a property name to use as a sink selector, a default value
to use when the property is not attached, and a function that configures the sinks based on each property value.

For example, when using _Serilog.Sinks.File_:

```powershell
dotnet add package Serilog.Sinks.File
```

The value of a log event property like `Name` can be inserted into log filenames:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Map("Name", "Other", (name, wt) => wt.File($"./logs/log-{name}.txt"))
    .CreateLogger();

Log.Information("Hello, {Name}!", "Alice");
// -> Event written to log-Alice.txt

Log.Information("Hello, {Name}!", "Bob");
// -> Event written to log-Bob.txt

Log.Information("Shutting down");
// -> Event written to log-Other.txt

Log.CloseAndFlush();
```

### Limiting the number of open sinks

By default, the target sinks opened by this sink won't be closed/disposed until the
mapped sink is. This is efficient for dispatching to a finite number of sinks,
e.g. file-per-log-level and so-on, but isn't suitable when the set of possible key values is
open-ended.

To limit the number of target sinks that will be kept open in the map, specify `sinkMapCountLimit`:

```csharp
    .WriteTo.Map("Name",
                 "Other",
                 (name, wt) => wt.File($"./logs/log-{name}.txt"),
                 sinkMapCountLimit: 10)
```

To keep no sinks open, i.e. close them immediately after processing each event, a `sinkMapCountLimit` of zero may be specified.

### Configuration with `<appSettings>` and `appSettings.json`

_Serilog.Sinks.Map_ is built around a mapping function, and as such, isn't able to be configured using XML or JSON configuration.
