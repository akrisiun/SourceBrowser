
# c:\bin\html\HtmlGenerator.exe /y  /out:../srcWeb/Index SourceBrowser.sln

# C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\
$env:VSINSTALLDIR = "C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools"
$env:VisualStudioVersion = "15.0"
$env:MSBUILD_EXE_PATH = "$env:VSINSTALLDIR\MSBuild\15.0\Bin\MSBuild.exe"
$env:CscToolPath="C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\bin\Roslyn"

.\src\HtmlGenerator\bin\Debug\net48\HtmlGenerator.exe  /y  /out:../srcWeb/Index SourceBrowser.sln