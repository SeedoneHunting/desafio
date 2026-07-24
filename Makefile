.PHONY: up demo load-test down test

up:
	@./scripts/start.sh

demo:
	@./scripts/demo.sh

load-test:
	@./scripts/load-test.sh

down:
	@docker compose --env-file .env down

test:
	@dotnet test Cashflow.sln --nologo
