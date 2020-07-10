#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["app/Piping.Server.App/Piping.Server.App.csproj", "app/Piping.Server.App/"]
COPY ["src/Piping.Server.Mvc.Pipe/Piping.Server.Mvc.Pipe.csproj", "src/Piping.Server.Mvc.Pipe/"]
COPY ["src/Piping.Server.Mvc/Piping.Server.Mvc.csproj", "src/Piping.Server.Mvc/"]
COPY ["src/Piping.Server.Core/Piping.Server.Core.csproj", "src/Piping.Server.Core/"]
COPY ["src/Piping.Server.Mvc.Commands/Piping.Server.Mvc.Commands.csproj", "src/Piping.Server.Mvc.Commands/"]
COPY ["src/Piping.Server.Extensions/Piping.Server.Extensions.csproj", "src/Piping.Server.Extensions/"]
RUN dotnet restore "app/Piping.Server.App/Piping.Server.App.csproj"
COPY . .
WORKDIR "/src/app/Piping.Server.App"
RUN dotnet build "Piping.Server.App.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Piping.Server.App.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Piping.Server.App.dll"]