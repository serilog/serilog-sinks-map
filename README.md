# Serilog.Sinks.Map [![Build status](https://ci.appveyor.com/api/projects/status/m7svuyb4bkve3q6y?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-map)

A Serilog sink wrapper that to a set of sinks keyed on a property value.

### Getting started

Install the package from NuGet:

```powershell
Install-Package Serilog.Sinks.Map -Pre
```

The `WriteTo.Map()` method accepts a property name to use as a sink selector, and
a function that configures the sinks based on each property value.

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Map("Name", (name, wt) => wt.RollingFile($"./logs/log-{name}-{{Date}}.txt"))
    .CreateLogger();

Log.Information("Hello, {Name}!", "Alice");
// -> Event written to log-Alice-20170606.txt

Log.Information("Hello, {Name}!", "Bob");
// -> Event written to log-Bob-20170606.txt

Log.CloseAndFlush();
```

**Important:** the target sinks opened by this sink won't be closed/disposed until the
mapped sink is. This means the library is useful for dispatching to a finite number of sinks,
e.g. file-per-log-level and so-on, but isn't suitable when the set of possible key values is
open-ended.
