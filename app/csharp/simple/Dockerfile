FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["./simple.csproj", "./"]
RUN dotnet restore "./simple.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "simple.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "simple.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# COPY .env .
ENV AZURE_TRACING_GEN_AI_CONTENT_RECORDING_ENABLED=true
ENV AZURE_EXPERIMENTAL_ENABLE_ACTIVITY_SOURCE=true
ENTRYPOINT ["dotnet", "simple.dll"]
