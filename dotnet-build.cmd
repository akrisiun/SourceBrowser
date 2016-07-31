dotnet restore
cd src\Common\
dotnet build

cd ..\BuildLogParser\
dotnet build

cd ..\HtmlGenerator\
dotnet build
dotnet publish -f net46 -o ../../bin/HtmlGenerator/

@set msbuild="%ProgramFiles(x86)%\msbuild\14.0\Bin\MSBuild.exe"
@if not exist %msbuild% @set msbuild="%ProgramFiles%\MSBuild\14.0\Bin\MSBuild.exe"
@if not exist %msbuild% @set msbuild="%ProgramFiles(x86)%\MSBuild\12.0\Bin\MSBuild.exe"
@if not exist %msbuild% @set msbuild="%ProgramFiles%\MSBuild\12.0\Bin\MSBuild.exe"

cd ..\SourceIndexServer
%msbuild% /v:m SourceIndexServerWeb.csproj

cd ..\HtmlGenerator.Tests
%msbuild% /v:m HtmlGenerator.Tests.csproj

cd ..\SourceIndexServer.Tests
%msbuild% /v:m SourceIndexServer.Tests.csproj

@cd ..\..\
@PAUSE