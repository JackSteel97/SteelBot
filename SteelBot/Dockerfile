FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SteelBot/SteelBot.csproj", "SteelBot/"]
RUN dotnet restore "SteelBot/SteelBot.csproj"
COPY . .
WORKDIR "/src/SteelBot"
RUN dotnet build "SteelBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SteelBot.csproj" --self-contained true -c $BUILD_CONFIGURATION -o /app/publish -r linux-arm64 /p:PublishSingleFile=true /p:DebugType=None /p:DebugSymbols=false 

FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim-arm64v8 AS final
ENV DOTNET_RUNNING_IN_CONTAINER=true \
        COMPlus_EnableDiagnostics=0
ENV STEELBOTENVIRONMENT=Test

USER $APP_UID
WORKDIR /app
COPY --from=publish --chown=app:app /app/publish ./
ENTRYPOINT ["./SteelBot"]
