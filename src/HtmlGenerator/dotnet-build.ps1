
$os = [Environment]::OSVersion.VersionString

if ($os.StartsWith("Unix"))
{
    # dotnet : 2.0.0-preview1-005977/Microsoft.Common.CurrentVersion.targets(1111,5): 
    # error MSB3644: The reference assemblies for framework ".NETFramework,Version=v4.6"
    msbuild /v:m HtmlGenerator.csproj
}
else { 
    # win10, osx
    dotnet build HtmlGenerator.csproj -o $pwd\bin
}

# dotnet build src\SourceIndexServer\SourceIndexServer.csproj  -o $pwd\web
