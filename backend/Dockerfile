# --- build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore ./Presentation/Presentation.csproj
RUN dotnet publish ./Presentation/Presentation.csproj -c Release -o /app/publish

# --- runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5123
EXPOSE 5123

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Presentation.dll"]