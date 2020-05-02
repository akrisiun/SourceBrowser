
# $msbuild = "c:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
# & $msbuild /v:m /p:Configuration=Debug src\HtmlGenerator\HtmlGenerator.csproj `
  # /property:GenerateFullPaths=true /p:DeployOnBuild=true /p:PublishProfile=$PWD\src\HtmlGenerator\Properties\PublishProfiles\FolderProfile.pubxml
  
dotnet publish -c Debug src\HtmlGenerator\HtmlGenerator.csproj `
   /p:PublishProfile=$PWD\src\HtmlGenerator\Properties\PublishProfiles\FolderProfile.pubxml `
   -o c:\bin\html

#  /t:Publish 
#  /p:DeployOnBuild=true;PublishProfile=<NameOfPublishProfile> 
#  dotnet publish -c Release /p:PublishProfile="Properties\PublishProfiles\CustomProfile.pubxml" does 