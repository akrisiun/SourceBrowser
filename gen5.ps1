# // mono --debug --debugger-agent=transport=dt_socket,server=y,address=127.0.0.1:55555 bin/

$os = [Environment]::OSVersion.VersionString
Write-Host "$os Debug:"

if ($os.StartsWith("Unix")) {

    $agent="transport=dt_socket,server=y,address=127.0.0.1:55555"

    Write-Host "mono --debug --debugger-agent=$agent  bin/HtmlGenerator.exe"
    mono --debug --debugger-agent=$agent  bin/HtmlGenerator.exe -debug -force SourceBrowser.sln  /out:web5e
}
else {
    bin/HtmlGenerator.exe -debug SourceBrowser.sln  /out:web5
}

# bin\HtmlGenerator.exe -debug SourceBrowser.sln  /out:web5
# @PAUSE