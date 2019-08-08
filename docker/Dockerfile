FROM microsoft/dotnet:1.1-runtime-deps

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    && apt-get install -y curl mc nano

# Install .NET Core
ENV DOTNET_VERSION 1.1.2
# https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/1.1.2/dotnet-debian-x64.1.1.2.tar.gz
ENV DOTNET_DOWNLOAD_URL https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/$DOTNET_VERSION/dotnet-debian-x64.$DOTNET_VERSION.tar.gz

RUN curl -SL $DOTNET_DOWNLOAD_URL --output dotnet.tar.gz \
    && mkdir -p /usr/share/dotnet \
    && tar -zxf dotnet.tar.gz -C /usr/share/dotnet \
    && rm dotnet.tar.gz \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet

ADD web /web/
WORKDIR /web
EXPOSE 5000

# RUN dotnet restore
ENV ASPNETCORE_ENVIRONMENT=Development
ENV FOLDER=/web/index
CMD dotnet Microsoft.SourceBrowser.SourceIndexServer.dll
