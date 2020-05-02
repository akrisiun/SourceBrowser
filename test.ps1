echo self-test SourceBrowser.sln

# dotnet build src\HtmlGenerator.Tests -f net48
dotnet test src\HtmlGenerator.Tests -f net48  --no-restore --no-build -v:n