set dir=%~dp0
@set cfg=Debug

@echo -------------------------------------------------------------------
@echo myget.org build: HtmlGenerator
@REM @type myget.cmd
@echo -------------------------------------------------------------------

set MsBuildExe=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe   
set VsTestConsole=C:\Wonka\testrunners\VSTest.Console.12.0.30723.0\lib\vstest.console.exe   
set MsTestExe=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSTest.exe   

@if "%MsBuildExe%"=="" (
  @set MsBuildExe=%ProgramFiles%\MSBuild\15.0\Bin\MSBuild.exe
)

"%MsBuildExe%" /p:Configuration="%cfg%" /v:m /m src\Common\Common.csproj
"%MsBuildExe%" /p:Configuration="%cfg%" /v:m /m src\BuildLogParser\BuildLogParser.csproj
"%MsBuildExe%" /p:Configuration="%cfg%" /v:m /m src\Microsoft.Language.Xml\src\Microsoft.Language.Xml\Microsoft.Language.Xml.csproj
"%MsBuildExe%" /p:Configuration="%cfg%" /v:m /m src\HtmlGenerator\HtmlGenerator.csproj

:after
@if not ("%errorlevel%"=="0") (
   @goto failure
)

@echo -------------------------------------------------------------------
@echo Build success
@echo Test
@echo -------------------------------------------------------------------

call test.cmd

:failure