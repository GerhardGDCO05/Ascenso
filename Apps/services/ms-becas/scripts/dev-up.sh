#!/usr/bin/env bash
set -euo pipefail

repo_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)
cd "$repo_root"

make migrate

docker compose exec -T postgres sh -ec \
    'psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"' \
    < Apps/services/ms-becas/deploy/local/seed-dev.sql

compose_config=$(docker compose config --format json)
keycloak_port=$(jq -r \
    '.services.keycloak.ports[] | select((.target | tonumber) == 8080) | .published' \
    <<< "$compose_config")
dev_stubs_port=$(jq -r \
    '.services["dev-stubs"].ports[] | select((.target | tonumber) == 8080) | .published' \
    <<< "$compose_config")
realm_url="http://localhost:${keycloak_port}/realms/ascenso/.well-known/openid-configuration"
dev_stubs_url="http://localhost:${dev_stubs_port}/__admin/mappings"

for attempt in {1..30}; do
    if curl --fail --silent "$realm_url" --output /dev/null \
        && curl --fail --silent "$dev_stubs_url" --output /dev/null; then
        printf 'MS-BEC local listo; PostgreSQL migrado, datos semilla cargados y dependencias disponibles.\n'
        exit 0
    fi
    sleep 1
done

printf 'No se pudieron verificar el realm ascenso (%s) y los stubs locales (%s).\n' \
    "$realm_url" "$dev_stubs_url" >&2
exit 1
