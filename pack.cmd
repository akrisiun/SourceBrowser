dotnet pack src\MEF\MEF.csproj         -o ..\.. --include-symbols --verbosity m
dotnet pack src\Common\Common.csproj   -o ..\.. --include-symbols --verbosity m
dotnet pack src\BuildLogParser\BuildLogParser.csproj -o ..\.. --include-symbols --verbosity m

dotnet pack src\HtmlGenerator\HtmlGenerator.csproj  -o ..\..  --include-symbols --verbosity m
dotnet pack src\SourceIndexServer\SourceIndexServer.csproj -o ..\..  --include-symbols --verbosity m

@PAUSE