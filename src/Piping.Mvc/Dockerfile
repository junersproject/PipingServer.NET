FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["PipingServer.NET/PipingServer.NET.csproj", "PipingServer.NET/"]
RUN dotnet restore "PipingServer.NET/PipingServer.NET.csproj"
COPY . .
WORKDIR "/src/PipingServer.NET"
RUN dotnet build "PipingServer.NET.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "PipingServer.NET.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "PipingServer.NET.dll"]