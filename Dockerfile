# Multi-stage build — produces a small runtime image with no SDK/build tools included.
# Cloud-agnostic: this image runs unmodified on any container host (App Service, ECS,
# Cloud Run, a bare VPS with Docker, etc.) — see DEPLOYMENT.md for the required
# environment variables and a full deployment checklist.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["AshtavinayakApp/AshtavinayakAPP.csproj", "AshtavinayakApp/"]
RUN dotnet restore "AshtavinayakApp/AshtavinayakAPP.csproj"

COPY ["AshtavinayakApp/", "AshtavinayakApp/"]
WORKDIR /src/AshtavinayakApp
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Document storage root — mount a volume here in production so uploaded agent
# documents survive container restarts/redeploys (see DEPLOYMENT.md's callout on this).
RUN mkdir -p /app/data/documents
ENV DocumentStorage__RootPath=/app/data/documents

# Data Protection key ring — encrypts the admin panel's session cookie. Mount a volume
# here too, otherwise every container restart invalidates every logged-in admin session.
RUN mkdir -p /app/data/keys
ENV DataProtection__KeysPath=/app/data/keys

ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "AshtavinayakAPP.dll"]
