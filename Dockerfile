FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/Domain/MultiVendor.Ecommerce.Domain.csproj           src/Domain/
COPY src/Application/MultiVendor.Ecommerce.Application.csproj src/Application/
COPY src/Infrastructure/MultiVendor.Ecommerce.Infrastructure.csproj src/Infrastructure/
COPY src/Api/MultiVendor.Ecommerce.Api.csproj                  src/Api/

RUN dotnet restore src/Api/MultiVendor.Ecommerce.Api.csproj

COPY . .

RUN dotnet publish src/Api/MultiVendor.Ecommerce.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "MultiVendor.Ecommerce.Api.dll"]
