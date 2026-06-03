# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_ENVIRONMENT=Development

# ──────────────────────────────────────────────
# Этап сборки Frontend: Client Interface
# ──────────────────────────────────────────────
FROM node:20-alpine AS client-build
WORKDIR /app
COPY frontend/client/client-interface/package*.json ./
RUN npm ci --no-cache
COPY frontend/client/client-interface/ .
RUN rm -rf node_modules/.vite .vite dist && npm run build:prod

# ──────────────────────────────────────────────
# Этап сборки Frontend: User Interface
# ──────────────────────────────────────────────
FROM node:20-alpine AS user-build
WORKDIR /app
COPY frontend/user/user-interface/package*.json ./
RUN npm ci --no-cache
COPY frontend/user/user-interface/ .
RUN rm -rf node_modules/.vite .vite dist && npm run build:prod

# ──────────────────────────────────────────────
# Этап сборки .NET backend
# ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["backend/src/Api/Api.csproj", "src/Api/"]
COPY ["backend/src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["backend/src/Application/Application.csproj", "src/Application/"]
COPY ["backend/src/Domain/Domain.csproj", "src/Domain/"]
RUN dotnet restore "./src/Api/Api.csproj"
COPY backend/ .
WORKDIR "/src/src/Api"
RUN dotnet build "./Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ──────────────────────────────────────────────
# Финальный образ
# ──────────────────────────────────────────────
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Копируем собранные фронтенд-сборки из stages
COPY --from=client-build /app/dist ./wwwroot/client
COPY --from=user-build /app/dist ./wwwroot/app
ENTRYPOINT ["dotnet", "Api.dll"]
