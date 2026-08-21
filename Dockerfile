FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Instacord.csproj ./
RUN dotnet restore Instacord.csproj
COPY . ./
RUN dotnet publish Instacord.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "Instacord.dll"]