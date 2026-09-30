FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY frontend/src/WebApp.Blazor/WebApp.Blazor.csproj frontend/src/WebApp.Blazor/
RUN dotnet restore frontend/src/WebApp.Blazor/WebApp.Blazor.csproj

COPY frontend/src/WebApp.Blazor/ frontend/src/WebApp.Blazor/

ARG API_BASE_URL
RUN test -n "$API_BASE_URL"
RUN sed -i "s#\"ApiBaseUrl\": \"[^\"]*\"#\"ApiBaseUrl\": \"${API_BASE_URL}\"#" \
    frontend/src/WebApp.Blazor/wwwroot/appsettings.json && \
    sed -i "s#connect-src 'self' http://localhost:5000 https://localhost:5000 http://localhost:5173 https://localhost:5173#connect-src 'self' ${API_BASE_URL}#" \
    frontend/src/WebApp.Blazor/wwwroot/index.html

RUN dotnet publish frontend/src/WebApp.Blazor/WebApp.Blazor.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM nginx:1.27-alpine AS final
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 8080
