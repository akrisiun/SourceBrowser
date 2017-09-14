cd ../src/SourceIndexServer

dotnet publish -c Release -o ../../docker/web/ -f netcoreapp1.1

@PAUSE