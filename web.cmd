@ECHO http://localhost:58088
set dir=%~dp0srcweb

"%ProgramFiles%\IIS Express\IISExpress.exe" /port:58088 /path:%dir% /clr:4.0 /systray:true /ntlm

@PAUSE