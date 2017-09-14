mkdir src
set dir=%~dp0

..\src\csLibCheck\bin\libcheck.exe  -store full src -file System.Collections.Immutable* -full %dir%

@REM -file "%dir%System.Collections.Immutable.dll" -out src

@PAUSE

