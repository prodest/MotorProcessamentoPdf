# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM registry.prodest.es.gov.br/microsoft/dotnet/aspnet:5.0-alpine AS base
WORKDIR /app
EXPOSE 8080
        
ARG DT_CUSTOM_PROP

ENV DT_CUSTOM_PROP=$DT_CUSTOM_PROP

# This stage is used to build the service project
FROM registry.prodest.es.gov.br/microsoft/dotnet/sdk:5.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["API/API.csproj", "API/"]
COPY ["Business/Business.csproj", "Business/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
RUN dotnet restore "./API/API.csproj"
COPY . .
WORKDIR "/src/API"
RUN dotnet nuget disable source nuget.org && dotnet nuget add source https://nexus-cicd-tools.apps.pr.k8s.es.gov.br/repository/nuget.org-proxy/index.json -n nexus
RUN dotnet build "./API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "API.dll"]