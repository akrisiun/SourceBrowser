set dir=%~dp0
@set cfg=Debug

dotnet restore SourceBrowser.sln

dotnet build SourceBrowser.sln --no-restore -c Debug

:after
@if not ("%errorlevel%"=="0") (
   @goto failure
)

@echo -------------------------------------------------------------------
@echo Build success
@echo Test
@echo -------------------------------------------------------------------

powershell -f test.ps1

:failure