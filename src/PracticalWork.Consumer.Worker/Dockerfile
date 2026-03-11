FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/PracticalWork.Consumer.Worker/PracticalWork.Consumer.Worker.csproj", "src/PracticalWork.Consumer.Worker/"]
COPY ["src/PracticalWork.Library.Cache.Redis/PracticalWork.Library.Cache.Redis.csproj", "src/PracticalWork.Library.Cache.Redis/"]
COPY ["src/PracticalWork.Library.Data.Minio/PracticalWork.Library.Data.Minio.csproj", "src/PracticalWork.Library.Data.Minio/"]
COPY ["src/PracticalWork.Library.Data.PostgreSql/PracticalWork.Library.Data.PostgreSql.csproj", "src/PracticalWork.Library.Data.PostgreSql/"]
COPY ["src/PracticalWork.Library.Data.Reports.PostgreSql/PracticalWork.Library.Data.Reports.PostgreSql.csproj", "src/PracticalWork.Library.Data.Reports.PostgreSql/"]
COPY ["src/PracticalWork.Library.MessageBroker/PracticalWork.Library.MessageBroker.csproj", "src/PracticalWork.Library.MessageBroker/"]
COPY ["src/PracticalWork.Library/PracticalWork.Library.csproj", "src/PracticalWork.Library/"]
RUN dotnet restore "./src/PracticalWork.Consumer.Worker/PracticalWork.Consumer.Worker.csproj"
COPY . .
WORKDIR "/src/src/PracticalWork.Consumer.Worker"
RUN dotnet build "./PracticalWork.Consumer.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PracticalWork.Consumer.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PracticalWork.Consumer.Worker.dll"]
