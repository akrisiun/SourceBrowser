# powershell web.ps1
# ps | grep Server.dll

$Env:FOLDER = "$PWD/Index"

Write-Host "FOLDER=$Env:FOLDER"

$dll = "$pwd/SourceIndexServer.dll"
# Write-Host $dll

if (![System.IO.File]::Exists($dll)) {
    Write-Host "$Env:PWD/bin\Microsoft.SourceBrowser.SourceIndexServer.exe"
    bin\Microsoft.SourceBrowser.SourceIndexServer.exe
}
else {  
    Write-Host "Server: $pwd SourceIndexServer.dll"
    Write-Host "http://localhost:5001/index.html#q="
    $urls = "http://*:5000"
    # $global:p += Start-Process dotnet -ArgumentList "SourceIndexServer.dll" -PassThru
    Start-Process dotnet -ArgumentList "SourceIndexServer.dll --urls $urls" -PassThru
}

