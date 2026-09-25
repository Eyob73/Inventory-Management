# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and restore dependencies
COPY ["Inventory-Management.slnx", "./"]
COPY ["Inventory-Management.Api/Inventory-Management.Api.csproj", "Inventory-Management.Api/"]
COPY ["Inventory-Management.Application/Inventory-Management.Application.csproj", "Inventory-Management.Application/"]
COPY ["Inventory-Management.Domain/Inventory-Management.Domain.csproj", "Inventory-Management.Domain/"]
COPY ["Inventory-Management.Infrastructure/Inventory-Management.Infrastructure.csproj", "Inventory-Management.Infrastructure/"]
RUN dotnet restore "Inventory-Management.Api/Inventory-Management.Api.csproj"

# Copy the remaining source code and build
COPY . .
WORKDIR "/src/Inventory-Management.Api"
RUN dotnet build "Inventory-Management.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "Inventory-Management.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Run
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Render assigns a port dynamically via the PORT environment variable.
# We configure ASP.NET Core to listen on this port.
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

ENTRYPOINT ["dotnet", "Inventory-Management.Api.dll"]
