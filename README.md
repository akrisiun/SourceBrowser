# SourceBrowser

This repository is clone of https://github.com/KirillOsenkov/SourceBrowser  
and https://github.com/natemcmaster/AspNetSourceBrowser .

### build.ps1 problem
```
PS>.\build.ps1
VERBOSE: GET https://github.com/aspnet/PlatformAbstractions/archive/master.zip
with 0-byte payload
VERBOSE: received -1-byte response of content type application/zip
% : The term 'Expand-Archive' is not recognized as the name of a cmdlet,
function, script file, or operable program. Check the spelling of the name, or
if a path was included, verify that the path is correct and try again.
At D:\webstack\Mvc\SourceBrowser\build.ps1:20 char:10
+ $repos | % {
+          ~~~
    + CategoryInfo          : ObjectNotFound: (Expand-Archive:String) [ForEach
   -Object], CommandNotFoundException
    + FullyQualifiedErrorId : CommandNotFoundException,Microsoft.PowerShell.Co
   mmands.ForEachObjectCommand

PS>
```
Source browser is c# based website generator that powers http://referencesource.microsoft.com and http://source.roslyn.io.
.NET Compiler Platform ("Roslyn") source available: http://source.roslyn.codeplex.com/
http://source.roslyn.codeplex.com/#Microsoft.Build.Tasks.CodeAnalysis/MSBuildTask.csproj

## Extended version SourceBrowser

Myget build status  
[![andriusk MyGet Build Status](https://www.myget.org/BuildSource/Badge/andriusk?identifier=1739964b-be8c-442f-a02c-270d08b595e4)](https://www.myget.org/)

This version includes more .csproj content files:    
XML: .config, .xml, .xslt, .json  
Razor Views: .cshtml    
Html styles, scripts: .css, .js     

# VS 15 Preview 3 build error

```
Severity	Code	Description	Project	File	Line	Suppression State
Warning		The imported project "C:\bin\VS15Preview\MSBuild\15.0\Bin\..\..\..\MSBuild\Microsoft\VisualStudio\v15.0\WebApplications\Microsoft.WebApplication.targets" 
was not found. Also, tried to find "WebApplications\Microsoft.WebApplication.targets" 
in the fallback search path(s) for $(VSToolsPath) - "C:\Program Files (x86)\MSBuild\Microsoft\VisualStudio\v15.0" . 
These search paths are defined in "%HOMEDIR%\AppData\Local\Microsoft\VisualStudio\15.0_F2CD9ECDA9DE4311FD91\devenv.exe.config". 
Confirm that the path in the <Import> declaration is correct, and that the file exists on disk in one of the search paths.	HtmlGenerator	D:\webstack\Mvc\SourceBrowser\src\SourceIndexServer\SourceIndexServer.csproj	435	
```

## Submodules

Microsoft.Language.Xml is clone of https://github.com/KirillOsenkov/XmlParser
```
git submodule add https://github.com/akrisiun/Microsoft.Language.Xml src/Microsoft.Language.Xml
git submodule add https://github.com/akrisiun/csLibCheck.git src/csLibCheck
```  
 
## Host your own static HTML website 

 Source Browser allows you to browse its own source code:
[http://sourcebrowser.azurewebsites.net](http://sourcebrowser.azurewebsites.net)

Orinal package available on NuGet:
[https://www.nuget.org/packages/Microsoft.SourceBrowser](https://www.nuget.org/packages/Microsoft.SourceBrowser)

##Project status and contributions

This is a reference implementation that showcases the concepts and Roslyn usage. It comes with no guarantees, use at your own risk. We will consider accepting high-quality pull requests that add non-trivial value, however we have no plans to do significant work on the application in its current form. Any significant rearchitecture, adding large features, big refactorings won't be accepted because of resource constraints. Feel free to use it to generate websites for your own code, integrate in your CI servers etc. Feel free to do whatever you want in your own forks. Bug reports are gratefully accepted.

For any questions, feel free to reach out to [@KirillOsenkov](https://twitter.com/KirillOsenkov) on Twitter. Thanks to [@v2_matveev](https://twitter.com/v2_matveev) for contributing TypeScript support!
