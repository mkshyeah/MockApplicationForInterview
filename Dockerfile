# ЭТАП 1: Базовый образ для запуска приложения (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
USER app
WORKDIR /app
EXPOSE 8080

# ЭТАП 2: Образ со всеми инструментами сборки (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем файл проекта и восстанавливаем зависимости
COPY ["AccountingHelper.API.csproj", "AccountingHelper.API/"]
RUN dotnet restore "AccountingHelper.API/AccountingHelper.API.csproj"

# Копируем остальные файлы и собираем проект
COPY . .
WORKDIR "/src/AccountingHelper.API"
RUN dotnet build "AccountingHelper.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ЭТАП 3: Публикация (оптимизация и обрезка лишнего)
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AccountingHelper.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ЭТАП 4: Финальный легковесный образ для продакшена
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AccountingHelper.API.dll"]