# powershell web.ps1
# ps | grep Server.dll

$Env:FOLDER = "/Users/andriusk/Sites/System.Web3/Index"
Write-Host "FOLDER=$Env:FOLDER"
# [Environment]::GetEnvironmentVariable("PATH","User")

$dll = "$pwd/SourceIndexServer.dll"
# Write-Host $dll

if (![System.IO.File]::Exists($dll)) {
    Write-Host "$Env:PWD/bin/Debug/netcoreapp1.1/SourceIndexServer.dll"
    dotnet bin/Debug/netcoreapp1.1/SourceIndexServer.dll
}
else {  
    Write-Host "Server: $pwd SourceIndexServer.dll"
    Write-Host "http://localhost:5001/index.html#q="
    $urls = "http://*:5000"
    # $global:p += Start-Process dotnet -ArgumentList "SourceIndexServer.dll" -PassThru
    Start-Process dotnet -ArgumentList "SourceIndexServer.dll --urls $urls" -PassThru
}

