# ── Stage 1: Build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first so layer cache is reused on code-only changes
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/PayrollSaaS.Shared/PayrollSaaS.Shared.csproj             src/PayrollSaaS.Shared/
COPY src/PayrollSaaS.Domain/PayrollSaaS.Domain.csproj             src/PayrollSaaS.Domain/
COPY src/PayrollSaaS.Application/PayrollSaaS.Application.csproj   src/PayrollSaaS.Application/
COPY src/PayrollSaaS.Infrastructure/PayrollSaaS.Infrastructure.csproj src/PayrollSaaS.Infrastructure/
COPY src/PayrollSaaS.API/PayrollSaaS.API.csproj                   src/PayrollSaaS.API/

RUN dotnet restore src/PayrollSaaS.API/PayrollSaaS.API.csproj

# Copy everything else and publish
COPY . .
RUN dotnet publish src/PayrollSaaS.API/PayrollSaaS.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Default port for Render (override via PORT env var if needed)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PayrollSaaS.API.dll"]
