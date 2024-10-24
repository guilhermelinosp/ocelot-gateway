FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore Ocelot.Gateway/*.csproj
COPY . .
WORKDIR "/Ocelot.Gateway"
RUN dotnet build *.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish *.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ocelot.Gateway.dll"]
