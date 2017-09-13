# sample docker run 

set FOLDER=%~dp0index

docker run -d -p 0.0.0.0:8055:5000 -v %FOLDER%:/web/index  --name src8055 akrisiun/dotnet-src1.2:latest

@PAUSE