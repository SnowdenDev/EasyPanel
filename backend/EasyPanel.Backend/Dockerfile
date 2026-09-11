# The Backend is plain ASP.NET Core with no Windows-specific code (Job Objects and Native
# AOT live only in the daemon, which never runs in this image) — a normal Linux container
# works fine here.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY contracts/EasyPanel.Contracts/EasyPanel.Contracts.csproj contracts/EasyPanel.Contracts/
COPY backend/EasyPanel.Backend/EasyPanel.Backend.csproj backend/EasyPanel.Backend/
RUN dotnet restore backend/EasyPanel.Backend/EasyPanel.Backend.csproj

COPY contracts/EasyPanel.Contracts/ contracts/EasyPanel.Contracts/
COPY backend/EasyPanel.Backend/ backend/EasyPanel.Backend/
RUN dotnet publish backend/EasyPanel.Backend/EasyPanel.Backend.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Plain HTTP inside the container — TLS termination is the reverse proxy's job (see
# docs/deployment.md), not this image's.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "EasyPanel.Backend.dll"]
