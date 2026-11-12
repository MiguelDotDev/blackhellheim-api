#!/usr/bin/env bash
# Sonda de salud del contenedor del API (historia 005). Hace GET <ruta> a 127.0.0.1:8080 con
# /dev/tcp (la imagen no trae curl) y sale con 0 solo si responde 200. Uso: sonda-http.sh /salud/base
ruta="${1:-/salud}"
exec 3<>/dev/tcp/127.0.0.1/8080 || exit 1
printf 'GET %s HTTP/1.0\r\nHost: localhost\r\nConnection: close\r\n\r\n' "$ruta" >&3
read -r -t 3 linea <&3 || exit 1
[[ "$linea" == HTTP/*" 200 "* ]]
