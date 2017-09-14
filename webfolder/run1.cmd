mkdir src
set dir=%~dp0

..\bin\Debug\Microsoft.SourceBrowser.HtmlGenerator.Tests.exe 

@REM ..\src\csLibCheck\bin\libcheck.exe  -store full src -file Microsoft.CodeAnalysis* -full %dir%
@REM -file "%dir%System.Collections.Immutable.dll" -out src

@PAUSE

