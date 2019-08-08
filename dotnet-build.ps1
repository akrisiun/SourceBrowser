
$os = [Environment]::OSVersion.VersionString

if ($os.StartsWith("Unix"))
{
    dotnet restore src/HtmlGenerator/HtmlGenerator.csproj
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