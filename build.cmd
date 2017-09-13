
nuget restore SourceBrowser.sln

@set msbuild="%ProgramFiles(x86)%\msbuild\15.0\Bin\MSBuild.exe"

%msbuild% SourceBrowser.sln /v:m /m

@PAUSE