@ECHO http://localhost:5001
set FOLDER=%~dp0web5\index

cd web5\
"c:\Program Files\dotnet\dotnet.exe" ..\web\Microsoft.SourceBrowser.SourceIndexServer.dll  --urls "http://*:5001"

@PAUSE