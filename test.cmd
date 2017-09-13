
if not exist "bin\Debug\net46\Microsoft.SourceBrowser.HtmlGenerator.Tests.exe" (
   dotnet build src\HtmlGenerator.Tests\HtmlGenerator.Tests.csproj
)
:next1

@REM if not exists("TestCode\TestSolution\App.config") 
 @REM @copy "TestCode\TestSolution\App.config.md" "TestCode\TestSolution\App.config"
@REM if not exists("TestCode\TestSolution\web.config.md" 
 @REM @copy "TestCode\TestSolution\App.config.md"     "TestCode\TestSolution\web.config"
@REM if not exists("TestCode\TestSolution\Views\web.config.md")
 @REM @copy "TestCode\TestSolution\Views\web.config.md" "TestCode\TestSolution\Views\web.config"

set TestWindow=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow
@if not exist "%TestWindow%" set TestWindow=%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow

"%TestWindow%\vstest.console.exe" bin\Debug\net46\Microsoft.SourceBrowser.HtmlGenerator.Tests.exe

dotnet test src\SourceIndexServer.Tests\SourceIndexServer.Tests.csproj

@PAUSE