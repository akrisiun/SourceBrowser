@echo off
setlocal enabledelayedexpansion
cd ..

set MSBUILD="c:\bin\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe"
if not exist %MSBUILD% set MSBUILD="c:\Program Files (x86)\MSBuild\15.0\Bin\MSBuild.exe"
@REM dotnet restore src\MSBuild.sln 

@echo %MSBUILD%
%MSBUILD% /v:m  msbuild\src\Framework\Microsoft.Build.Framework.csproj

%MSBUILD% /v:m  msbuild\src\Build\Microsoft.Build.csproj

dotnet build roslyn\src\Workspaces\CSharp\Portable\CSharpWorkspace.csproj 

@REM dotnet build 
%MSBUILD% /v:m  roslyn\src\Workspaces\Core\Desktop\Workspaces.Desktop.csproj

dotnet build SourceBrowser\src\HtmlGenDebug\HtmlGenDebug.csproj
@PAUSE    