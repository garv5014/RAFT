FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY *.sln ./
COPY ../../RAFT/*.csproj ./RAFT/
COPY ../../RAFT_Gateway/*.csproj ./RAFT_Gateway/
COPY ../../RAFT_Node/*.csproj ./RAFT_Node/
COPY ../../RAFT_Test/*.csproj ./RAFT_Test/



RUN dotnet restore RAFT.sln

COPY . ./
RUN dotnet build RAFT.sln -c Release -o out