@ECHO http://localhost:55055
@set FOLDER=%~dp0
@echo %FOLDER%

"c:\Program Files\dotnet\dotnet.exe" Microsoft.SourceBrowser.SourceIndexServer.dll  --urls "http://*:55055"

@PAUSE