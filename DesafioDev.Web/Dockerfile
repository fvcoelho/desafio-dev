# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["DesafioDev.Web/DesafioDev.Web.csproj", "DesafioDev.Web/"]
RUN dotnet restore "DesafioDev.Web/DesafioDev.Web.csproj"

# Copy everything else and build
COPY DesafioDev.Web/ DesafioDev.Web/
WORKDIR "/src/DesafioDev.Web"
RUN dotnet build "DesafioDev.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DesafioDev.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DesafioDev.Web.dll"]
