@ECHO http://localhost:5000
set FOLDER=%~dp0
echo %FOLDER%
set ASPNETCORE_ENVIRONMENT=Development

"c:\Program Files\dotnet\dotnet.exe" SourceIndexServer.dll 

@REM --urls "http://*:5000"

@PAUSE