
@REM if not exists("TestCode\TestSolution\App.config") 
 @REM @copy "TestCode\TestSolution\App.config.md" "TestCode\TestSolution\App.config"
@REM if not exists("TestCode\TestSolution\web.config.md" 
 @REM @copy "TestCode\TestSolution\App.config.md"     "TestCode\TestSolution\web.config"
@REM if not exists("TestCode\TestSolution\Views\web.config.md")
 @REM @copy "TestCode\TestSolution\Views\web.config.md" "TestCode\TestSolution\Views\web.config"


@set TestWindow=%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow
@if not exist "%TestWindow%" set TestWindow=C:\Program Files\Microsoft Visual Studio 12.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow

"%TestWindow%\vstest.console.exe"  bin\Microsoft.SourceBrowser.HtmlGenerator.Tests\Microsoft.SourceBrowser.HtmlGenerator.Tests.dll
"%TestWindow%\vstest.console.exe"  bin\Microsoft.SourceBrowser.SourceIndexServer.Tests\Microsoft.SourceBrowser.SourceIndexServer.Tests.dll

@PAUSE