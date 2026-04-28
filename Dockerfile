FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["AiChatApi.csproj", "./"]
RUN dotnet restore "AiChatApi.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "AiChatApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AiChatApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser && chown -R appuser:appuser /app
USER appuser

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 5000

# Copy published app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "AiChatApi.dll"]