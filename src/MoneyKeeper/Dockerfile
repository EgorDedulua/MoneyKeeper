FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/MoneyKeeper/MoneyKeeper.csproj", "MoneyKeeper/"]
COPY ["src/MoneyKeeper.Infrastructure/MoneyKeeper.Infrastructure.csproj", "MoneyKeeper.Infrastructure/"]
COPY ["src/MoneyKeeper.Application/MoneyKeeper.Application.csproj", "MoneyKeeper.Application/"]
COPY ["src/MoneyKeeper.Core/MoneyKeeper.Core.csproj", "MoneyKeeper.Core/"]
RUN dotnet restore "MoneyKeeper/MoneyKeeper.csproj"
COPY src/ /src/
WORKDIR /src/MoneyKeeper
RUN dotnet build "./MoneyKeeper.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MoneyKeeper.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MoneyKeeper.dll"]