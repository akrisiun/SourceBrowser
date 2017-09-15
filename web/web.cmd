@ECHO http://localhost:58088
set FOLDER=%~dp0Index
echo %FOLDER%
set ASPNETCORE_ENVIRONMENT=Development

"c:\Program Files\dotnet\dotnet.exe" SourceIndexServer.dll  --urls "http://*:58088"

@REM --server.urls http://0.0.0.0:5001

@PAUSE