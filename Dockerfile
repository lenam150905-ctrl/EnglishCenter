FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["EnglishCenter.Gateway.csproj", "."]
RUN dotnet restore "EnglishCenter.Gateway.csproj"
COPY . .
RUN dotnet publish "EnglishCenter.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
COPY ocelot.docker.json ./ocelot.json
ENTRYPOINT ["dotnet", "EnglishCenter.Gateway.dll"]
