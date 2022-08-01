# Stage 1 - Build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS builder

WORKDIR /app/src

# Restore
COPY *.csproj .

RUN dotnet restore 
# Build
COPY . .

RUN dotnet publish -c Release -o /app/publish --no-self-contained 

# Stage 2 - Publish
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

RUN addgroup --system --gid 101 app \
    && adduser --system --ingroup app --uid 101 app


COPY --from=builder --chown=app:app /app/publish .

USER app
    
ENTRYPOINT ["./ProtoCom.Api"]