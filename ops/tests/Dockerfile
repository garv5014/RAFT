FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY *.sln ./
COPY RAFT/*.csproj ./RAFT/
COPY Raft_Gateway/*.csproj ./Raft_Gateway/
COPY Raft_Node/*.csproj ./Raft_Node/
COPY RAFT_Test/*.csproj ./RAFT_Test/
COPY Raft_Library/*.csproj ./Raft_Library/
COPY Raft_Shop/*.csproj ./Raft_Shop/
COPY Raft_Shop.Client/*.csproj ./Raft_Shop.Client/

RUN dotnet restore RAFT.sln

COPY . ./
RUN dotnet build RAFT.sln -c Release -o out