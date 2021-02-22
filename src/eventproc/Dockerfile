FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR "/src"
COPY eventproc/ eventproc/
COPY shared/ shared/

WORKDIR "/src/eventproc"
RUN dotnet restore "eventproc.csproj"
RUN dotnet build "eventproc.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "eventproc.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "eventproc.dll"]
