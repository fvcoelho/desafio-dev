# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["DesafioDev.Web/DesafioDev.Web.csproj", "DesafioDev.Web/"]
COPY ["DesafioDev.Api/DesafioDev.Api.csproj", "DesafioDev.Api/"]
COPY ["DesafioDev.Api.Tests/DesafioDev.Api.Tests.csproj", "DesafioDev.Api.Tests/"]
RUN dotnet restore "DesafioDev.Web/DesafioDev.Web.csproj"
RUN dotnet restore "DesafioDev.Api.Tests/DesafioDev.Api.Tests.csproj"

# Copy everything else
COPY DesafioDev.Web/ DesafioDev.Web/
COPY DesafioDev.Api/ DesafioDev.Api/
COPY DesafioDev.Api.Tests/ DesafioDev.Api.Tests/
COPY CNAB.txt CNAB.txt

# Test stage - run tests and generate HTML report
FROM build AS test
WORKDIR /src
RUN dotnet test DesafioDev.Api.Tests/DesafioDev.Api.Tests.csproj \
    --configuration Release \
    --logger "html;LogFileName=test-results.html" \
    --results-directory /testresults \
    || true

# Build Web project
FROM build AS webbuild
WORKDIR "/src/DesafioDev.Web"
RUN dotnet build "DesafioDev.Web.csproj" -c Release -o /app/build

# Publish stage
FROM webbuild AS publish
RUN dotnet publish "DesafioDev.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
# Copy test report to wwwroot for web access
COPY --from=test /testresults/ ./wwwroot/test-report/
ENTRYPOINT ["dotnet", "DesafioDev.Web.dll"]
