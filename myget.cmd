set dir=%~dp0
@set cfg=Debug

@echo -------------------------------------------------------------------
@echo myget.org build: HtmlGenerator
@REM @type myget.cmd
@echo -------------------------------------------------------------------

@if "%MsBuildExe%"=="" (
  @set MsBuildExe=%ProgramFiles%\MSBuild\14.0\Bin\MSBuild.exe
)

"%MsBuildExe%" /p:Configuration="%cfg%" /v:m /m src\Common\Common.csproj
"%MsBuildExe%" /p:Configuration="%cfg%" /v:m /m src\BuildLogParser\BuildLogParser.csproj
"%MsBuildExe%" /p:Configuration="%cfg%" /v:m /m src\Microsoft.Language.Xml\src\Microsoft.Language.Xml\Microsoft.Language.Xml.csproj
"%MsBuildExe%" /p:Configuration="%cfg%" /v:m /m src\HtmlGenerator\HtmlGenerator.csproj

:after
@if not ("%errorlevel%"=="0") (
   @goto failure
)

:failure