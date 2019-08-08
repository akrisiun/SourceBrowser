#!/bin/sh
# sample docker run 
# 

docker run -d -p 0.0.0.0:8080:5000 -v /Users/andriusk/Sites/srcMvc5/Index:/web/index  --name b8080 akrisiun/dotnet-src1.2:latest