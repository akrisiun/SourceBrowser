
$os = [Environment]::OSVersion.VersionString

if ($os.StartsWith("Unix"))
{
    # dotnet : 2.0.0-preview1-005977/Microsoft.Common.CurrentVersion.targets(1111,5): 
    # error MSB3644: The reference assemblies for framework ".NETFramework,Version=v4.6"

    dotnet restore src/HtmlGenerator/HtmlGenerator.csproj
    # dotnet msbuild   src/HtmlGenerator/HtmlGenerator.csproj
    dotnet build   src/HtmlGenerator/HtmlGenerator.csproj -o $PWD/bin --no-restore --no-dependencies --force

    $agent="transport=dt_socket,server=y,address=127.0.0.1:55555"
    // mono --debug --debugger-agent=$agent  bin/HtmlGenerator.exe -debug -force SourceBrowser.sln  /out:web5e
}
else { 
    dotnet build src\HtmlGenerator\HtmlGenerator.csproj -o ..\..\bin
}

dotnet build src\SourceIndexServer\SourceIndexServer.csproj  -o ..\..\web

# @PAUSE
# Console.ReadKey()