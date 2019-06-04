FROM microsoft/dotnet:aspnetcore-runtime
COPY ./ /docker
WORKDIR /docker
ENTRYPOINT ["dotnet", "StorageProxy.dll"]