#!/usr/bin/env python3
"""Genera el fixture local de Keycloak desde el realm compartido."""

import argparse
import json
from pathlib import Path


RAIZ = Path(__file__).resolve().parents[1]
BASE = RAIZ / "deploy/keycloak/ascenso-realm.json"
LOCAL = RAIZ / "Apps/services/ms-becas/deploy/keycloak/ascenso-realm.json"
DATOS_LOCALES = RAIZ / "Apps/services/ms-becas/deploy/local/keycloak-overlay.json"
CAMBIOS_CLIENTE_PERMITIDOS = {
    "ms-bec": {"directAccessGrantsEnabled", "redirectUris", "webOrigins"},
    "ms-bec-internal": {"secret"},
}


def generar() -> str:
    realm = json.loads(BASE.read_text(encoding="utf-8"))
    datos = json.loads(DATOS_LOCALES.read_text(encoding="utf-8"))

    if realm.get("realm") != "ascenso" or "users" in realm:
        raise ValueError("El realm compartido debe ser ascenso y no contener usuarios")
    if set(datos) != {"users", "clients"}:
        raise ValueError("El overlay local solo admite users y clients")
    if not isinstance(datos["users"], list) or not isinstance(datos["clients"], dict):
        raise ValueError("El overlay local tiene una estructura inválida")

    clientes = {cliente["clientId"]: cliente for cliente in realm["clients"]}
    for nombre, valores in datos["clients"].items():
        if (
            nombre not in clientes
            or nombre not in CAMBIOS_CLIENTE_PERMITIDOS
            or not isinstance(valores, dict)
            or not set(valores) <= CAMBIOS_CLIENTE_PERMITIDOS[nombre]
        ):
            raise ValueError(f"Cambios locales no permitidos para el cliente: {nombre}")
        clientes[nombre].update(valores)

    realm["users"] = datos["users"]
    return json.dumps(realm, ensure_ascii=False, indent=2) + "\n"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="verifica que el fixture esté actualizado")
    args = parser.parse_args()
    contenido = generar()
    if args.check:
        if LOCAL.read_text(encoding="utf-8") != contenido:
            raise SystemExit("El realm local está desactualizado: ejecuta scripts/generar-realm-local.py")
    else:
        LOCAL.write_text(contenido, encoding="utf-8")


if __name__ == "__main__":
    main()
