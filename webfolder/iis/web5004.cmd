@ECHO http://localhost:5004
@REM set FOLDER=%~dp0web3\wwwroot
set FOLDER=%~dp0index

"c:\Program Files\dotnet\dotnet.exe" Microsoft.SourceBrowser.SourceIndexServer.dll  --urls "http://*:5004"

@PAUSE