#powershell
#!/bin/bash

$Env:FOLDER = "/Users/andriusk/Sites/System.Web3"
Write-Host "FOLDER=$Env:FOLDER"

# [Environment]::GetEnvironmentVariable("PATH","User")
if (![System.IO.File]::Exists("SourceIndexServer.dll")) {
    Write-Host "$Env:PWD bin/SourceIndexServer.dll"
    dotnet bin/Debug/netcoreapp1.1/SourceIndexServer.dll
}
else {  
    Write-Host "Server: $Env:PWD SourceIndexServer.dll"
    dotnet SourceIndexServer.dll  --urls "http://*:5001"
}

