# Imagen del API de BlackHellHeim (historia 005). Multietapa y reproducible:
# bases oficiales de Microsoft fijadas por digest, restauración con dependencias bloqueadas
# y configuración inyectada en tiempo de ejecución (nunca incrustada en la imagen).
# Compatible con el constructor clásico de Docker (sin BuildKit ni buildx).
ARG IMAGEN_SDK=mcr.microsoft.com/dotnet/sdk:10.0@sha256:fe0fd4e93bd2fbb47c883b7a6369e5c8d3db8bf01cdc5981f6ee35fbdf3ff4fb
ARG IMAGEN_ASPNET=mcr.microsoft.com/dotnet/aspnet:10.0@sha256:bfd912c83295ce66d4b706ebb8541b9f14259b0e76fbb88b2751f58ce2399d2e
ARG IMAGEN_RUNTIME_DEPS=mcr.microsoft.com/dotnet/runtime-deps:10.0@sha256:9cab4d7d886226d47b5489ef9abf0544e2f109fde11a8acc190217610af26c00

# --- Restauración: solo manifiestos, para reutilizar la capa mientras no cambien -------------
FROM ${IMAGEN_SDK} AS restauracion
WORKDIR /fuente
COPY global.json Directory.Build.props ./
COPY .config/dotnet-tools.json .config/dotnet-tools.json
COPY src/Dominio/BlackHellHeim.Dominio.csproj src/Dominio/packages.lock.json src/Dominio/
COPY src/Aplicacion/BlackHellHeim.Aplicacion.csproj src/Aplicacion/packages.lock.json src/Aplicacion/
COPY src/Infraestructura/BlackHellHeim.Infraestructura.csproj src/Infraestructura/packages.lock.json src/Infraestructura/
COPY src/Api/BlackHellHeim.Api.csproj src/Api/packages.lock.json src/Api/
RUN dotnet restore src/Api/BlackHellHeim.Api.csproj --locked-mode \
 && dotnet tool restore

# --- Compilación: publica el API y genera el bundle de migraciones -------------------------
FROM restauracion AS compilacion
ARG TARGETARCH
COPY src/Dominio src/Dominio
COPY src/Aplicacion src/Aplicacion
COPY src/Infraestructura src/Infraestructura
COPY src/Api src/Api
RUN dotnet publish src/Api/BlackHellHeim.Api.csproj -c Release --no-restore -p:UseAppHost=false -o /app/publicar \
 && rm -f /app/publicar/appsettings.Development.json
# FabricaContextoDiseno exige la variable de conexión: se pasa una ficticia, sin credenciales,
# solo en este comando (no es ENV ni ARG, así que no llega a las etapas siguientes).
RUN case "${TARGETARCH:-amd64}" in \
      amd64) RID=linux-x64 ;; \
      arm64) RID=linux-arm64 ;; \
      *) echo "Arquitectura no soportada: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
 && ConnectionStrings__BlackHellHeim="Server=localhost;Database=BlackHellHeimBundle;Integrated Security=true;Encrypt=False" \
    dotnet ef migrations bundle --self-contained -r "$RID" --configuration Release \
      -p src/Infraestructura -s src/Infraestructura --force -o /app/migraciones/efbundle

# --- Migraciones: aplica el esquema y las semillas; la conexión llega por variable --------
FROM ${IMAGEN_RUNTIME_DEPS} AS migraciones
WORKDIR /app
COPY --from=compilacion /app/migraciones/efbundle ./efbundle
RUN chmod 555 ./efbundle
USER app
ENTRYPOINT ["/bin/sh", "-c", "exec ./efbundle --connection \"$ConnectionStrings__BlackHellHeim\""]

# --- Final: solo el runtime y la publicación, sin SDK ni fuentes, sin privilegios ----------
FROM ${IMAGEN_ASPNET} AS final
ARG VERSION=local
ARG REVISION=desconocida
LABEL org.opencontainers.image.title="BlackHellHeim API" \
      org.opencontainers.image.source="https://github.com/MiguelDotDev/blackhellheim-api" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.revision="${REVISION}" \
      org.opencontainers.image.licenses="MIT"
WORKDIR /app
COPY --from=compilacion /app/publicar ./
COPY docker/sonda-http.sh /app/sonda-http.sh
RUN chmod 555 /app/sonda-http.sh
USER app
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "BlackHellHeim.Api.dll"]
