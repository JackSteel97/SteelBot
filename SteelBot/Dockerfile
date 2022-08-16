FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SteelBot/SteelBot.csproj", "SteelBot/"]
RUN dotnet restore "SteelBot/SteelBot.csproj"
COPY . .
WORKDIR "/src/SteelBot"
RUN dotnet build "SteelBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SteelBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SteelBot.dll"]
