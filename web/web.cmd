@ECHO http://localhost:58088
set dir=%~dp0
set FOLDER=%dir%\wwwroot
echo %dir% WEB: %folder%

set ASPNETCORE_ENVIRONMENT=Development

"c:\Program Files\dotnet\dotnet.exe" Microsoft.SourceBrowser.SourceIndexServer.dll  --urls "http://*:58088"

@REM --server.urls http://0.0.0.0:5001

@PAUSE