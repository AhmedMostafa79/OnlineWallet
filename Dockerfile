# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#define a variable that carries build configuration
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for dependency restoration
COPY ["src/OnlineWallet.API/OnlineWallet.API.csproj", "OnlineWallet.API/"]
COPY ["src/OnlineWallet.Application/OnlineWallet.Application.csproj", "OnlineWallet.Application/"]
COPY ["src/OnlineWallet.Domain/OnlineWallet.Domain.csproj", "OnlineWallet.Domain/"]
COPY ["src/OnlineWallet.Infrastructure/OnlineWallet.Infrastructure.csproj", "OnlineWallet.Infrastructure/"]

# Restore NuGet packages
# Mentioned .API only as it has all dependecies
RUN dotnet restore "OnlineWallet.API/OnlineWallet.API.csproj"

# Copy all source files
COPY src/ .

# Build the project
WORKDIR "/src/OnlineWallet.API"
RUN dotnet build "OnlineWallet.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 2: Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "OnlineWallet.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Expose ports (HTTP and HTTPS)
EXPOSE 8080
EXPOSE 8081

# Copy published application from publish stage
COPY --from=publish /app/publish .

# Set environment to Production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Run the application
ENTRYPOINT ["dotnet", "OnlineWallet.API.dll"]