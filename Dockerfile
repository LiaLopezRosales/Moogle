FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MoogleEngine/MoogleEngine.csproj MoogleEngine/
COPY MoogleServer/MoogleServer.csproj MoogleServer/
COPY MoogleTests/MoogleTests.csproj MoogleTests/
COPY Moogle.sln .
RUN dotnet restore

COPY . .
RUN dotnet restore && dotnet publish MoogleServer/MoogleServer.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app/publish
COPY --from=build /app/publish .
COPY Content/ /app/Content/

EXPOSE 8080

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "MoogleServer.dll"]
