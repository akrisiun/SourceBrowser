
cd MSBuildStructuredLog\

@REM nuget install spat
nuget install squirrel.windows
cd MSBuildStructuredLog\src\StructuredLogger\
dotnet restore
cd ..\

@set msbuild="%ProgramFiles(x86)%\msbuild\14.0\Bin\MSBuild.exe"
@if not exist %msbuild% @set msbuild="%ProgramFiles%\MSBuild\14.0\Bin\MSBuild.exe"
@if not exist %msbuild% @set msbuild="%ProgramFiles(x86)%\MSBuild\12.0\Bin\MSBuild.exe"
@if not exist %msbuild% @set msbuild="%ProgramFiles%\MSBuild\12.0\Bin\MSBuild.exe"

%msbuild% /v:m MSBuildStructuredLog.sln

@PAUSE