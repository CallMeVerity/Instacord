FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG RID=linux-x64
WORKDIR /src
COPY src/Instacord.csproj ./
RUN dotnet restore Instacord.csproj -r $RID
COPY src/ ./
RUN dotnet publish Instacord.csproj -c Release -r $RID --self-contained -o /app

FROM debian:bookworm-slim
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends libicu-dev ca-certificates \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app ./
ENTRYPOINT ["./Instacord"]