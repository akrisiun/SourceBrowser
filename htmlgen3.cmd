set dir=%~dp0

%dir%\Html\HtmlGenerator.exe "%1" /out:websrc /fast

@PAUSE