# Dinocollab.LoggerProvider

[![NuGet](https://img.shields.io/nuget/v/Dinocollab.LoggerProvider.svg)](https://www.nuget.org/packages/Dinocollab.LoggerProvider)

Dinocollab.LoggerProvider is a lightweight logging provider for .NET applications. It can forward logs to supported backends (for example, QuestDB). This README explains installation, package metadata required for the NuGet "Get" link, and provides a usage example.

Repository: https://github.com/dinolibraries/Dinocollab.LoggerProvider

## NuGet "Get" link
The NuGet package page shows a "Get" button and project links based on package metadata in the project file. Make sure your `*.csproj` contains the following properties so the NuGet page links back to this repository:

```xml
<PropertyGroup>
  <PackageProjectUrl>https://github.com/dinolibraries/Dinocollab.LoggerProvider</PackageProjectUrl>
  <RepositoryUrl>https://github.com/dinolibraries/Dinocollab.LoggerProvider</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
</PropertyGroup>
```

Note: `PackageReadmeFile` has been added to the library project so this README will be included in the NuGet package.

## Installation
Install from NuGet with the `dotnet` CLI:

```bash
dotnet add package Dinocollab.LoggerProvider
```

Or add a `PackageReference` to your project file:

```xml
<ItemGroup>
  <PackageReference Include="Dinocollab.LoggerProvider" Version="*" />
</ItemGroup>
```

## Usage
Register the QuestDB logger provider in `Program.cs` (example):

```csharp
builder.Services.AddQuestDBLoggerProvider(option =>
{
    option.ConnectionString = "tcp::addr=localhost:9009;";
    //option.ConnectionString = "http::addr=localhost:9000;";
    option.ApiUrl = "http://localhost:9000";
    option.TableLogName = "berlintomek";
});

app.UseQuestDBLoggerProvider();
```

Notes:
- `ConnectionString` may use `tcp::addr=host:port;` or `http::addr=host:port;` depending on transport.
- `ApiUrl` is the HTTP endpoint (if using the HTTP API).
- `TableLogName` is the destination table/stream name for logs.

See the `Dinocollab.LoggerProvider/QuestDB` folder for helper classes and configuration options.

## Contributing
Contributions are welcome — please open issues or submit pull requests on GitHub.

## License
See the `LICENSE` file in this repository for license information. If you'd like, I can add a license file.

---

Would you like me to commit these changes now?

  *** End Patch

