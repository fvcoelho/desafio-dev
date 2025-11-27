# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["DesafioDev.Api/DesafioDev.Api.csproj", "DesafioDev.Api/"]
RUN dotnet restore "DesafioDev.Api/DesafioDev.Api.csproj"

# Copy everything else and build
COPY DesafioDev.Api/ DesafioDev.Api/
WORKDIR "/src/DesafioDev.Api"
RUN dotnet build "DesafioDev.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DesafioDev.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DesafioDev.Api.dll"]
