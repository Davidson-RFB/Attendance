# syntax=docker/dockerfile:1

# Build context is the repository root (see docker-compose.yml / CI workflow),
# so all paths below are relative to the repo root.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy just the project files first so package restore is cached across
# builds unless a .csproj actually changes.
COPY DavidsonRFB.Attendance.slnx .
COPY DavidsonRFB.Attendance.Web/DavidsonRFB.Attendance.Web.csproj DavidsonRFB.Attendance.Web/
COPY DavidsonRFB.Attendance.Web.Tests/DavidsonRFB.Attendance.Web.Tests.csproj DavidsonRFB.Attendance.Web.Tests/
RUN dotnet restore DavidsonRFB.Attendance.Web/DavidsonRFB.Attendance.Web.csproj

COPY DavidsonRFB.Attendance.Web/ DavidsonRFB.Attendance.Web/
RUN dotnet publish DavidsonRFB.Attendance.Web/DavidsonRFB.Attendance.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Azure Web App for Containers: set the WEBSITES_PORT app setting to 8080
# to match this. Locally (docker-compose) this is mapped directly.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DavidsonRFB.Attendance.Web.dll"]
