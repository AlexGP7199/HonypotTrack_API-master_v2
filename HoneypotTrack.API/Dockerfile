FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["HoneypotTrack.API/HoneypotTrack.API.csproj", "HoneypotTrack.API/"]
COPY ["HonypotTrack.Application/HonypotTrack.Application.csproj", "HonypotTrack.Application/"]
COPY ["HoneypotTrack.Infrastrcture/HoneypotTrack.Infrastrcture.csproj", "HoneypotTrack.Infrastrcture/"]
COPY ["HoneypotTrack.Domain/HoneypotTrack.Domain.csproj", "HoneypotTrack.Domain/"]
RUN dotnet restore "./HoneypotTrack.API/HoneypotTrack.API.csproj"

COPY . .
WORKDIR "/src/HoneypotTrack.API"
RUN dotnet build "./HoneypotTrack.API.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HoneypotTrack.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HoneypotTrack.API.dll"]
