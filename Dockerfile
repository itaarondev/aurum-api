# ==========================================
# 1. ETAPA DE COMPILACIÓN (SDK de .NET)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos la solución
COPY ["examples.slnx", "./"]

# Copiamos los proyectos (Api, Core, Infrastructure)
COPY ["src/Api/Aurum.Api/Aurum.Api.csproj", "src/Api/Aurum.Api/"]
COPY ["src/Core/Aurum.Application/Aurum.Application.csproj", "src/Core/Aurum.Application/"]
COPY ["src/Core/Aurum.Domain/Aurum.Domain.csproj", "src/Core/Aurum.Domain/"]
COPY ["src/Infrastructure/Aurum.External/Aurum.External.csproj", "src/Infrastructure/Aurum.External/"]
COPY ["src/Infrastructure/Aurum.Persistence/Aurum.Persistence.csproj", "src/Infrastructure/Aurum.Persistence/"]

# Restauramos dependencias
RUN dotnet restore "src/Api/Aurum.Api/Aurum.Api.csproj"

# Copiamos todo el código y compilamos
COPY . .
WORKDIR "/src/src/Api/Aurum.Api"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# 2. ETAPA DE EJECUCIÓN (Runtime)
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Aurum.Api.dll"]