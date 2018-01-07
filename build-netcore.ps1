[cmdletbinding(PositionalBinding = $false)]
param(
    [string]$Config='Release'
)

$ErrorActionPreference='Stop'

$repos = Get-Content $PSScriptRoot/repos.txt
$branch='master'
$buildDir = Join-Path $PSScriptRoot bin
$artifacts = Join-Path $PSScriptRoot artifacts
# remove-item $buildDir -Recurse

remove-item $artifacts -Recurse

mkdir $buildDir -ErrorAction Ignore | Out-Null
mkdir $buildDir/zip -ErrorAction Ignore | Out-Null
mkdir $buildDir/master -ErrorAction Ignore | Out-Null

$repos | % { 
    $unzip = Join-Path $buildDir "src/$_"
    $zip = Join-Path $buildDir "zip/$_.zip"
    if (!(Test-Path $zip)) {
        iwr https://github.com/aspnet/$_/archive/$branch.zip -OutFile $zip -Verbose
    }
    if (!(Test-Path $unzip)) {
        Write-Verbose "Unzipping $zip"
        $dest=Split-Path -Parent $unzip
        Expand-Archive -Path $zip -DestinationPath $dest
        Move-Item $dest/$_-$branch $unzip
    }

    Get-ChildItem $unzip/src/* -Directory |
        # filter test projects in src/
        ? { !($_ -like '*.Testing' -or $_ -like '*.Tests') } |
        ? { 
            if (!(Test-Path $_/project.json)) {
                return $True
            }

            $p = Get-Content -Raw $_/project.json | ConvertFrom-Json
            # filter projects who are only netcoreapps, like dotnet-razor-tooling
            $isNetCoreApp=($p.frameworks | Get-Member -MemberType NoteProperty -Name "netcoreapp1.0") -and ($p.frameworks | Get-Member -MemberType NoteProperty | Measure-Object).Count -eq 1
            return !$isNetCoreApp
        } |
        % {
            if (Test-Path $_) {
                Write-Host "Skipping '$_'. Already exists."
                return
            }
            Write-Verbose "Copying $_"
            Copy-Item $_ -Recurse $buildDir/master
        }
}

'{}' | Out-File $buildDir/master/global.json

dotnet restore $buildDir/master

& 'C:\Program Files (x86)\msbuild\14.0\Bin\MSBuild.exe' $PSScriptRoot/SourceBrowser.sln /p:Configuration=$Config

& "$PSScriptRoot\bin\$Config\HtmlGenerator\HtmlGenerator.exe" $buildDir/master/global.json /out:$artifacts/website/