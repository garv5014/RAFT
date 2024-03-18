#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Raft_Shop/Raft_Shop/Raft_Shop.csproj", "Raft_Shop/Raft_Shop/"]
COPY ["Raft_Shop/Raft_Shop.Client/Raft_Shop.Client.csproj", "Raft_Shop/Raft_Shop.Client/"]
RUN dotnet restore "./Raft_Shop/Raft_Shop/./Raft_Shop.csproj"
COPY . .
WORKDIR "/src/Raft_Shop/Raft_Shop"
RUN dotnet build "./Raft_Shop.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Raft_Shop.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Raft_Shop.dll"]