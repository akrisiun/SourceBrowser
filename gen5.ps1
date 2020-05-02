# // mono --debug --debugger-agent=transport=dt_socket,server=y,address=127.0.0.1:55555 bin/

$os = [Environment]::OSVersion.VersionString
Write-Host "$os Debug:"


if ($os.StartsWith("Unix")) {
    $agent="transport=dt_socket,server=y,address=127.0.0.1:55555"

    Write-Host "mono --debug --debugger-agent=$agent  bin/HtmlGenerator.exe"
    mono --debug --debugger-agent=$agent  bin/HtmlGenerator.exe -debug -force SourceBrowser.sln  /out:web5e
}
else {
    # ./bin/HtmlGenerator.exe -debug SourceBrowser.sln  /out:web5
    
    $env:VSINSTALLDIR = "C:\Program Files (x86)\Microsoft Visual Studio\Preview\Community"
    $env:VisualStudioVersion = "15.0"
    $env:MSBUILD_EXE_PATH = "$env:VSINSTALLDIR\MSBuild\15.0\Bin\MSBuild.exe"
    $env:CscToolPath="C:\Program Files (x86)\Microsoft Visual Studio\Preview\Community\MSBuild\15.0\bin\Roslyn"

    # No idea why it doesn't pick up the other two variables you used.
    # $env:VSINSTALLDIR = "E:\Microsoft Visual Studio\Preview\Community"
    write-Host "MSB EXE: $env:MSBUILD_EXE_PATH"
    
    ./bin/HtmlGenerator.exe SourceBrowser.sln  /out:web5
}

# bin\HtmlGenerator.exe -debug SourceBrowser.sln  /out:web5
# @PAUSE