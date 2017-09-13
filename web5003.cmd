@ECHO http://localhost:5003
set FOLDER=%~dp0web3\wwwroot

cd %~dp0web3
"c:\Program Files\dotnet\dotnet.exe" Microsoft.SourceBrowser.SourceIndexServer.dll  --urls "http://*:5003"

@PAUSE