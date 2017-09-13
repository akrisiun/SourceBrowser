
@REM dotnet restore
@REM call msbuild15 D:\Beta\src2\roslyn\src\Workspaces\Core\Desktop\Workspaces.Desktop.csproj

@REM dotnet build D:\Beta\src2\roslyn\src\Workspaces\Core\Desktop\Workspaces.Desktop.csproj --no-dependencies

dotnet build   ..\roslyn\src\Workspaces\CSharp\Portable\CSharpWorkspace.csproj --no-dependencies
dotnet build   ..\roslyn\src\Workspaces\Core\Portable\Workspaces.csproj --no-dependencies
dotnet build   ..\roslyn\src\Workspaces\Core\Desktop\Workspaces.Desktop.csproj --no-dependencies 

dotnet publish D:\Beta\src2\roslyn\src\Workspaces\Core\Desktop\Workspaces.Desktop.csproj --no-dependencies -o d:\Beta\src2\roslyn\src\Workspaces\bin\

dotnet build src\Microsoft.Language.Xml\src\Microsoft.Language.Xml\Microsoft.Language.Xml.csproj

dotnet build src\Mef\Mef.csproj --no-dependencies

@REM   dotnet build
dotnet build src\HtmlGenerator\HtmlGenerator.csproj -o ..\..\bin  --no-dependencies

dotnet build src\HtmlGenDebug\HtmlGenDebug.csproj   --no-dependencies

@PAUSE