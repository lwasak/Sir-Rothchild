FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY . .
WORKDIR "/src/"
RUN dotnet publish "SirRothchild.sln" -c Release --runtime linux-musl-x64 -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime-deps:7.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SirRothchild.dll"]
