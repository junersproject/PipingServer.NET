#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["app/PipingServer.App/PipingServer.App.csproj", "app/PipingServer.App/"]
COPY ["src/PipingServer.Mvc.Pipe/PipingServer.Mvc.Pipe.csproj", "src/PipingServer.Mvc.Pipe/"]
COPY ["src/PipingServer.Mvc/PipingServer.Mvc.csproj", "src/PipingServer.Mvc/"]
COPY ["src/PipingServer.Core/PipingServer.Core.csproj", "src/PipingServer.Core/"]
COPY ["src/PipingServer.Mvc.Commands/PipingServer.Mvc.Commands.csproj", "src/PipingServer.Mvc.Commands/"]
COPY ["src/PipingServer.Extensions/PipingServer.Extensions.csproj", "src/PipingServer.Extensions/"]
RUN dotnet restore "app/PipingServer.App/PipingServer.App.csproj"
COPY . .
WORKDIR "/src/app/PipingServer.App"
RUN dotnet build "PipingServer.App.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PipingServer.App.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PipingServer.App.dll"]
