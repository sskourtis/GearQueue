FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Samples/WorkerExample/WorkerExample.csproj", "Samples/WorkerExample/"]
COPY ["GearQueue.Extensions.Microsoft.DependencyInjection/GearQueue.Extensions.Microsoft.DependencyInjection.csproj", "GearQueue.Extensions.Microsoft.DependencyInjection/"]
COPY ["GearQueue/GearQueue.csproj", "GearQueue/"]
COPY ["Samples/SampleUtils/SampleUtils.csproj", "Samples/SampleUtils/"]
RUN dotnet restore "Samples/WorkerExample/WorkerExample.csproj"
COPY . .
WORKDIR "/src/Samples/WorkerExample"
RUN dotnet build "./WorkerExample.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WorkerExample.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WorkerExample.dll"]
