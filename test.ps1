echo self-test SourceBrowser.sln
# dotnet build src\HtmlGenerator.Tests -f net461
dotnet test src\HtmlGenerator.Tests -f net461  --no-restore --no-build -v:n