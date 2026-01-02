FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OpenProfileServer.Generator/OpenProfileServer.Generator.csproj", "OpenProfileServer.Generator/"]
RUN dotnet restore "OpenProfileServer.Generator/OpenProfileServer.Generator.csproj"
COPY . .
WORKDIR "/src/OpenProfileServer.Generator"
RUN dotnet build "./OpenProfileServer.Generator.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OpenProfileServer.Generator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenProfileServer.Generator.dll"]
