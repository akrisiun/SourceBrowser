

nuget push  -source %USERPROFILE%\.nuget\packages\  Microsoft.Language.Xml.1.1.0.nupkg


cd src/Common
nuget restore project.json
dotnet pack -o ..\..

cd ..\..
nuget push  -source %USERPROFILE%\.nuget\packages\  Common.1.0.0.nupkg

nuget push  -source %USERPROFILE%\.nuget\packages\  BuildLogParser.1.1.0.nupkg

@PAUSE