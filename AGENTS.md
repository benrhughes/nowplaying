# Instructions for AI Agents - NowPlaying

## 🛡️ General Rules
- **Permissions**: NEVER run `scp`, `rsync`, or perform `git` write operations (commit, push, etc.) without explicit permission.
- **Workflow**: ALWAYS run `dotnet build` AND `dotnet test` after code changes.
- **Cleanup**: ALWAYS remove any temporary build or test output files created (e.g., `build_output_*.txt`, `test_output_*.txt`) once they are no longer needed.
- **Warnings**: ALWAYS fix compilation errors and StyleCop warnings immediately. Treat warnings as errors.

## 🚀 .NET 10 & Architecture
- **Framework**: ASP.NET Core Minimal APIs.
- **Modern Features**: Use `C# 14` features where appropriate, such as **Primary Constructors** for DI.
- **HTTP**: Always use **Typed Clients** (e.g., `services.AddHttpClient<IService, Service>()`) and avoid magic strings like `"Default"`.
- **Configuration**: Use `AppConfig.cs` for strongly-typed settings derived from environment variables.

## 📝 Coding Standards (StyleCop Alignment)
- **Documentation**: All public and protected members MUST have XML documentation (`/// <summary>`) to satisfy rule **SA1600**. Use `<inheritdoc/>` for implementation methods.
- **Spacing**: Maintain a single blank line between class members, including fields (satisfies **SA1516**).
- **Validation**: Use model binding and validation and the `ValidationFilter` for request validation on Minimal API endpoints.
- **JSON**: Prefer `System.Text.Json` over `Newtonsoft.Json`.
- **Supression**: do not suppress stylecop warnings without explicit approval

## 🧪 Testing
- **Framework**: xUnit and Moq.
- **Coverage**: ALWAYS add unit test coverage for new code changes in the `bcmasto.tests` project and strive to keep overall coverage above **80 %**.  After running `dotnet test` you can generate a report with:
  ```bash
  cd src
  dotnet test nowplaying.tests/nowplaying.tests.csproj --collect:"XPlat Code Coverage" --settings .runsettings
  # results are written to TestResults/<GUID>/coverage.cobertura.xml

  # Use the helper script to see a summary of coverage by class
  python3 coverage_summary.py nowplaying.tests/TestResults/<GUID>/coverage.cobertura.xml
  ```
  Tools like [ReportGenerator](https://github.com/danielpalme/ReportGenerator) or `coverlet`’s CLI can convert the XML into readable HTML.  Review metrics before merging changes.
- **Mocks**: When testing services that use `HttpClient`, use `MockHttpMessageHandler` and a real `HttpClient` instance rather than mocking `IHttpClientFactory`.
- **Frontend Tests**: Run `npm test -- --run` (NOT just `npm test`) to exit cleanly after tests complete. Without `--run`, vitest stays in watch mode and will hang. To check frontend coverage, run:
  ```bash
  npm run test:coverage -- --run
  # results are written to coverage/index.html
  ```

## 📖 Related Documentation
- **Onboarding**: Read [SETUP.md](SETUP.md) for environment configuration.
- **Architecture**: See [DEVELOPMENT.md](DEVELOPMENT.md) for deep-dive technical details.