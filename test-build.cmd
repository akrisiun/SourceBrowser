
@REM dotnet restore
set msbuild15="c:\Program Files (x86)\Microsoft Visual Studio\Preview\Community\MSBuild\15.0\Bin\MSBuild.exe"

@REM set msbuild15="c:\bin\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe"

@REM dotnet build
@REM dotnet build src\HtmlGenerator\HtmlGenerator.csproj -o ..\..\bin
@REM dotnet msbuild 

@REM %msbuild15%  /v:m D:\Beta\src2\roslyn\src\Workspaces\Core\Desktop\Workspaces.Desktop.csproj
@REM %msbuild15%  /v:m src\HtmlGenDebug\HtmlGenDebug.csproj

dotnet build   ..\roslyn\src\Workspaces\Core\Portable\Workspaces.csproj --no-dependencies
dotnet build   ..\roslyn\src\Compilers\Core\Portable\CodeAnalysis.csproj --no-dependencies
dotnet build   ..\roslyn\src\Compilers\CSharp\Portable\CSharpCodeAnalysis.csproj --no-dependencies
dotnet build   ..\roslyn\src\Workspaces\Core\Desktop\Workspaces.Desktop.csproj --no-dependencies 
dotnet build src\HtmlGenDebug\HtmlGenDebug.csproj   --no-dependencies

@PAUSE