FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["service-b.csproj", "."]
RUN dotnet restore "service-b.csproj"
COPY . .
RUN dotnet publish "service-b.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "service-b.dll"]