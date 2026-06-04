# Api/Dockerfile
# Build context must be the repo root (docker-compose sets context: .)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Api/DroneMesh3D.Api.csproj Api/
COPY Core/DroneMesh3D.Core.csproj Core/
RUN dotnet restore Api/DroneMesh3D.Api.csproj
COPY Api/ Api/
COPY Core/ Core/
RUN dotnet publish Api/DroneMesh3D.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "DroneMesh3D.Api.dll"]
