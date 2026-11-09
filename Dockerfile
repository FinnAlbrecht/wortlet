# Eine Anwendung, ein Container: das .NET-Backend liefert auch das gebaute React-Frontend aus.

# ---- Frontend bauen ----
FROM node:22-alpine AS frontend
WORKDIR /src
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY frontend/ ./
RUN npm run build

# ---- Backend bauen ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY backend/Directory.Build.props backend/Directory.Packages.props ./
COPY backend/src/Wortlet.Domain/Wortlet.Domain.csproj src/Wortlet.Domain/
COPY backend/src/Wortlet.Application/Wortlet.Application.csproj src/Wortlet.Application/
COPY backend/src/Wortlet.Infrastructure/Wortlet.Infrastructure.csproj src/Wortlet.Infrastructure/
COPY backend/src/Wortlet.Api/Wortlet.Api.csproj src/Wortlet.Api/
RUN dotnet restore src/Wortlet.Api/Wortlet.Api.csproj
COPY backend/src/ src/
RUN dotnet publish src/Wortlet.Api/Wortlet.Api.csproj -c Release -o /app --no-restore /p:UseAppHost=false

# ---- Laufzeit ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=backend /app ./
COPY --from=frontend /src/dist ./wwwroot
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Storage__DataDirectory=/data     Backup__Directory=/backups
RUN mkdir -p /data /backups && chown -R app:app /data /backups
USER app
VOLUME ["/data", "/backups"]
EXPOSE 8080
ENTRYPOINT ["dotnet", "Wortlet.Api.dll"]
