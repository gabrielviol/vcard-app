# syntax=docker/dockerfile:1

# --- Stage 1: build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar so o .csproj primeiro para aproveitar o cache de camada do Docker no restore --
# so invalida se as dependencias mudarem, nao a cada alteracao de codigo.
COPY Api.csproj .
RUN dotnet restore Api.csproj

# Copiar o restante do contexto (apps/api) e publicar. Api.Tests/ e artefatos locais
# (bin/, obj/, appsettings.Development.json) sao excluidos via .dockerignore.
COPY . .
RUN dotnet publish Api.csproj -c Release -o /app/publish --no-restore

# --- Stage 2: runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]
