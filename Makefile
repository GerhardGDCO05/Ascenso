COMPOSE ?= docker compose
DOTNET ?= dotnet

SOLUTION := Apps/services/ms-becas/Ascenso.Becas.sln
API_PROJECT := Apps/services/ms-becas/Ascenso.Becas.Api/Ascenso.Becas.Api.csproj
INFRASTRUCTURE_PROJECT := Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Ascenso.Becas.Infrastructure.csproj

.PHONY: up down migrate run test format dev-up

up:
	$(COMPOSE) up --wait -d

down:
	$(COMPOSE) --profile api down

dev-up:
	./Apps/services/ms-becas/scripts/dev-up.sh

migrate: up
	@set -eu; \
	config="$$($(COMPOSE) config --format json)"; \
	db=$$(printf '%s' "$$config" | jq -r '.services.postgres.environment.POSTGRES_DB'); \
	user=$$(printf '%s' "$$config" | jq -r '.services.postgres.environment.POSTGRES_USER'); \
	password=$$(printf '%s' "$$config" | jq -r '.services.postgres.environment.POSTGRES_PASSWORD'); \
	port=$$(printf '%s' "$$config" | jq -r '.services.postgres.ports[] | select((.target | tonumber) == 5432) | .published'); \
	ConnectionStrings__Becas="Host=localhost;Port=$$port;Database=$$db;Username=$$user;Password=$$password" \
		$(DOTNET) ef database update --project "$(INFRASTRUCTURE_PROJECT)" --startup-project "$(INFRASTRUCTURE_PROJECT)" --context BecasDbContext

run: dev-up
	@set -eu; \
	config="$$($(COMPOSE) --profile api config --format json)"; \
	value() { printf '%s' "$$config" | jq -r "$$1"; }; \
	db=$$(value '.services.postgres.environment.POSTGRES_DB'); \
	db_user=$$(value '.services.postgres.environment.POSTGRES_USER'); \
	db_password=$$(value '.services.postgres.environment.POSTGRES_PASSWORD'); \
	db_port=$$(value '.services.postgres.ports[] | select((.target | tonumber) == 5432) | .published'); \
	rabbit_user=$$(value '.services.rabbitmq.environment.RABBITMQ_DEFAULT_USER'); \
	rabbit_password=$$(value '.services.rabbitmq.environment.RABBITMQ_DEFAULT_PASS'); \
	rabbit_port=$$(value '.services.rabbitmq.ports[] | select((.target | tonumber) == 5672) | .published'); \
	keycloak_port=$$(value '.services.keycloak.ports[] | select((.target | tonumber) == 8080) | .published'); \
	dev_stubs_port=$$(value '.services["dev-stubs"].ports[] | select((.target | tonumber) == 8080) | .published'); \
	storage_provider=$$(value '.services.api.environment.Storage__Provider'); \
	firebase_bucket=$$(value '.services.api.environment.Storage__Firebase__Bucket'); \
	ConnectionStrings__Becas="Host=localhost;Port=$$db_port;Database=$$db;Username=$$db_user;Password=$$db_password" \
	RabbitMq__Host=localhost \
	RabbitMq__Port="$$rabbit_port" \
	RabbitMq__Username="$$rabbit_user" \
	RabbitMq__Password="$$rabbit_password" \
	Jwt__Authority="http://localhost:$$keycloak_port/realms/ascenso" \
	Jwt__Issuer="http://localhost:$$keycloak_port/realms/ascenso" \
	Keycloak__ServiceToken__Authority="http://localhost:$$keycloak_port/realms/ascenso" \
	Keycloak__ServiceToken__ClientId=ms-bec-internal \
	Keycloak__ServiceToken__ClientSecret=ms_bec_internal_local_only \
	Clientes__Postulaciones__BaseUrl="http://localhost:$$dev_stubs_port" \
	Clientes__Postulaciones__UsaServiceToken=false \
	Clientes__Programas__BaseUrl="http://localhost:$$dev_stubs_port" \
	Clientes__Programas__UsaServiceToken=false \
	Storage__Provider="$$storage_provider" \
	Storage__Firebase__Bucket="$$firebase_bucket" \
		ASPNETCORE_ENVIRONMENT=Development $(DOTNET) run --project "$(API_PROJECT)"

test:
	$(DOTNET) test "$(SOLUTION)" --configuration Release

format:
	$(DOTNET) format "$(SOLUTION)" --no-restore
