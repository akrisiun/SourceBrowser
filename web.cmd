@ECHO http://localhost:5001
set FOLDER=%~dp0web\wwwroot

@REM "%ProgramFiles%\IIS Express\IISExpress.exe" /port:58088 /path:%dir% /clr:4.0 /systray:true /ntlm
cd web\
"c:\Program Files\dotnet\dotnet.exe" Microsoft.SourceBrowser.SourceIndexServer.dll  --urls "http://*:5001"

@REM --server.urls http://0.0.0.0:5001

@PAUSE