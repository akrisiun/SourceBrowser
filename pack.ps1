
dotnet pack src/Common/Common.csproj  -o ../..   --include-source --include-symbols
dotnet pack src/Mef/Mef.csproj  -o ../..   --include-source --include-symbols
dotnet pack src/BuildLogParser/BuildLogParser.csproj  -o ../..   --include-source --include-symbols

dotnet pack src/HtmlGenerator/HtmlGenerator.csproj  -o ../..   --include-source --include-symbols --no-build
dotnet pack src/SourceIndexServer/SourceIndexServer.csproj -o ../..  --include-source --include-symbols
