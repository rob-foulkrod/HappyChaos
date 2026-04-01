FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY HappyChaos.Todo.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
    
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/out ./
ENTRYPOINT ["dotnet", "HappyChaos.Todo.dll"]
