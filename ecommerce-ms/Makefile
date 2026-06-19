# ════════════════════════════════════════════════════════════════════════════
#  ecommerce-ms — Makefile
#  Uso: make <target>
# ════════════════════════════════════════════════════════════════════════════

.PHONY: help up down restart build logs ps \
        migrate migrate-customer migrate-product migrate-order migrate-notification \
        migrate-rollback seed \
        test test-unit test-integration test-api test-watch \
        k6-smoke k6-load k6-stress k6-spike k6-soak k6-all \
        lint format restore clean \
        db-shell-customer db-shell-product db-shell-order \
        redis-cli rabbit-ui jaeger prometheus grafana \
        k8s-up k8s-down k8s-status k8s-logs k8s-test \
        outbox-status deadletter-requeue health-all

# ── Variáveis ────────────────────────────────────────────────────────────
COMPOSE         := docker compose
DOTNET          := dotnet
K6              := k6
KUBECTL         := kubectl
NAMESPACE       := ecommerce
RESULTS_DIR     := k6/results

CUSTOMER_URL    ?= http://localhost:5001
PRODUCT_URL     ?= http://localhost:5002
ORDER_URL       ?= http://localhost:5003
NOTIF_URL       ?= http://localhost:5004

GREEN  := \033[0;32m
YELLOW := \033[1;33m
CYAN   := \033[0;36m
BOLD   := \033[1m
NC     := \033[0m

## help: Lista todos os comandos disponíveis
help:
	@echo ""
	@echo "$(BOLD)$(CYAN)╔══════════════════════════════════════════════╗$(NC)"
	@echo "$(BOLD)$(CYAN)║        ecommerce-ms — Comandos               ║$(NC)"
	@echo "$(BOLD)$(CYAN)╚══════════════════════════════════════════════╝$(NC)"
	@echo ""
	@echo "$(BOLD)── AMBIENTE ──────────────────────────────────────$(NC)"
	@echo "  $(GREEN)make up$(NC)               Sobe toda a infraestrutura"
	@echo "  $(GREEN)make down$(NC)             Derruba tudo (preserva volumes)"
	@echo "  $(GREEN)make down-v$(NC)           Derruba tudo + apaga volumes"
	@echo "  $(GREEN)make restart$(NC)          Reinicia todos os serviços"
	@echo "  $(GREEN)make build$(NC)            Builda todas as imagens"
	@echo "  $(GREEN)make ps$(NC)               Status dos containers"
	@echo "  $(GREEN)make logs$(NC)             Logs de todos os serviços"
	@echo "  $(GREEN)make logs-svc SVC=xxx$(NC) Logs de um serviço específico"
	@echo ""
	@echo "$(BOLD)── BANCO DE DADOS ────────────────────────────────$(NC)"
	@echo "  $(GREEN)make migrate$(NC)          Roda migrations em todos os serviços"
	@echo "  $(GREEN)make migrate-customer$(NC) Migration apenas do Customer"
	@echo "  $(GREEN)make migrate-product$(NC)  Migration apenas do Product"
	@echo "  $(GREEN)make migrate-order$(NC)    Migration apenas do Order"
	@echo "  $(GREEN)make migrate-rollback$(NC) Reverte última migration (todos)"
	@echo "  $(GREEN)make seed$(NC)             Popula dados de teste"
	@echo "  $(GREEN)make db-shell-customer$(NC) Abre psql no banco do Customer"
	@echo "  $(GREEN)make db-shell-product$(NC)  Abre psql no banco do Product"
	@echo "  $(GREEN)make db-shell-order$(NC)    Abre psql no banco do Order"
	@echo ""
	@echo "$(BOLD)── TESTES ────────────────────────────────────────$(NC)"
	@echo "  $(GREEN)make test$(NC)             Roda todos os testes (unit + api)"
	@echo "  $(GREEN)make test-unit$(NC)        Apenas testes unitários (.NET)"
	@echo "  $(GREEN)make test-integration$(NC) Testes de integração"
	@echo "  $(GREEN)make test-api$(NC)         Testes de API end-to-end (bash)"
	@echo "  $(GREEN)make test-watch$(NC)       Testes em modo watch"
	@echo ""
	@echo "$(BOLD)── K6 LOAD TESTS ─────────────────────────────────$(NC)"
	@echo "  $(GREEN)make k6-smoke$(NC)         Smoke test (sanidade, 1 VU)"
	@echo "  $(GREEN)make k6-load$(NC)          Load test (carga normal, 50 VUs)"
	@echo "  $(GREEN)make k6-stress$(NC)        Stress test (limite, 200 VUs)"
	@echo "  $(GREEN)make k6-spike$(NC)         Spike test (pico repentino)"
	@echo "  $(GREEN)make k6-soak$(NC)          Soak test (resistência, 30 min)"
	@echo "  $(GREEN)make k6-all$(NC)           Roda smoke + load + stress"
	@echo "  $(GREEN)make k6-report$(NC)        Abre relatório HTML do último run"
	@echo ""
	@echo "$(BOLD)── KUBERNETES ────────────────────────────────────$(NC)"
	@echo "  $(GREEN)make k8s-up$(NC)           Aplica todos os manifests"
	@echo "  $(GREEN)make k8s-down$(NC)         Remove o namespace ecommerce"
	@echo "  $(GREEN)make k8s-status$(NC)       Status dos pods"
	@echo "  $(GREEN)make k8s-test$(NC)         Testes via port-forward"
	@echo "  $(GREEN)make k8s-logs SVC=xxx$(NC) Logs de um pod"
	@echo ""
	@echo "$(BOLD)── OBSERVABILIDADE ───────────────────────────────$(NC)"
	@echo "  $(GREEN)make health-all$(NC)       Checa /health de todos os serviços"
	@echo "  $(GREEN)make outbox-status$(NC)    Mostra mensagens pendentes no Outbox"
	@echo "  $(GREEN)make deadletter-requeue$(NC) Reprocessa mensagens DeadLetter"
	@echo "  $(GREEN)make rabbit-ui$(NC)        Abre RabbitMQ Management"
	@echo "  $(GREEN)make jaeger$(NC)           Abre Jaeger UI"
	@echo "  $(GREEN)make prometheus$(NC)       Abre Prometheus"
	@echo "  $(GREEN)make grafana$(NC)          Abre Grafana"
	@echo ""
	@echo "$(BOLD)── UTILITÁRIOS ───────────────────────────────────$(NC)"
	@echo "  $(GREEN)make lint$(NC)             Analisa código (.NET analyzers)"
	@echo "  $(GREEN)make format$(NC)           Formata código (dotnet format)"
	@echo "  $(GREEN)make restore$(NC)          Restaura pacotes NuGet"
	@echo "  $(GREEN)make clean$(NC)            Limpa bin/obj de todos os projetos"
	@echo ""

# ════════════════════════════════════════════════════════════════════════════
# AMBIENTE
# ════════════════════════════════════════════════════════════════════════════

## up: Sobe toda a infraestrutura e serviços
up:
	@echo "$(CYAN)▶ Subindo infraestrutura...$(NC)"
	$(COMPOSE) up -d --remove-orphans
	@echo "$(CYAN)▶ Aguardando healthchecks...$(NC)"
	@sleep 5
	@$(MAKE) health-all

## down: Derruba containers preservando volumes
down:
	$(COMPOSE) down

## down-v: Derruba containers e apaga volumes
down-v:
	$(COMPOSE) down -v
	@echo "$(YELLOW)⚠ Volumes removidos — próximo 'make up' vai recriar os bancos$(NC)"

## restart: Reinicia os microserviços (sem recriar infra)
restart:
	$(COMPOSE) restart customer-service product-service order-service notification-service

## build: Builda todas as imagens
build:
	$(COMPOSE) build --parallel

## ps: Status dos containers
ps:
	$(COMPOSE) ps

## logs: Logs de todos os serviços
logs:
	$(COMPOSE) logs -f customer-service product-service order-service notification-service

## logs-svc SVC=xxx: Logs de um serviço específico
logs-svc:
	$(COMPOSE) logs -f $(SVC)

# ════════════════════════════════════════════════════════════════════════════
# BANCO DE DADOS / MIGRATIONS
# ════════════════════════════════════════════════════════════════════════════

## migrate: Roda migrations em todos os serviços
migrate: migrate-customer migrate-product migrate-order migrate-notification
	@echo "$(GREEN)✔ Todas as migrations aplicadas$(NC)"

## migrate-customer: Migration do Customer
migrate-customer:
	@echo "$(CYAN)▶ Migration: Customer$(NC)"
	$(DOTNET) ef database update \
		--project src/Customer/Infrastructure/Customer.Infrastructure.csproj \
		--startup-project src/Customer/Api/Customer.Api.csproj \
		--context CustomerDbContext

## migrate-product: Migration do Product
migrate-product:
	@echo "$(CYAN)▶ Migration: Product$(NC)"
	$(DOTNET) ef database update \
		--project src/Product/Infrastructure/Product.Infrastructure.csproj \
		--startup-project src/Product/Api/Product.Api.csproj \
		--context ProductDbContext

## migrate-order: Migration do Order
migrate-order:
	@echo "$(CYAN)▶ Migration: Order$(NC)"
	$(DOTNET) ef database update \
		--project src/Order/Infrastructure/Order.Infrastructure.csproj \
		--startup-project src/Order/Api/Order.Api.csproj \
		--context OrderDbContext

## migrate-notification: Migration do Notification
migrate-notification:
	@echo "$(CYAN)▶ Migration: Notification$(NC)"
	$(DOTNET) ef database update \
		--project src/Notification/Infrastructure/Notification.Infrastructure.csproj \
		--startup-project src/Notification/Api/Notification.Api.csproj \
		--context NotificationDbContext

## migrate-add NAME=xxx SVC=customer: Cria nova migration
migrate-add:
	$(DOTNET) ef migrations add $(NAME) \
		--project src/$(shell echo $(SVC) | sed 's/./\u&/')/Infrastructure \
		--startup-project src/$(shell echo $(SVC) | sed 's/./\u&/')/Api \
		--context $(shell echo $(SVC) | sed 's/./\u&/')DbContext

## migrate-rollback: Reverte última migration em todos os serviços
migrate-rollback:
	@echo "$(YELLOW)⚠ Revertendo última migration...$(NC)"
	$(DOTNET) ef migrations remove --project src/Customer/Infrastructure/Customer.Infrastructure.csproj --startup-project src/Customer/Api/Customer.Api.csproj --context CustomerDbContext --force
	$(DOTNET) ef migrations remove --project src/Product/Infrastructure/Product.Infrastructure.csproj  --startup-project src/Product/Api/Product.Api.csproj   --context ProductDbContext  --force
	$(DOTNET) ef migrations remove --project src/Order/Infrastructure/Order.Infrastructure.csproj      --startup-project src/Order/Api/Order.Api.csproj         --context OrderDbContext    --force

## seed: Popula dados de teste via script
seed:
	@echo "$(CYAN)▶ Populando dados de teste...$(NC)"
	@bash scripts/seed.sh

## db-shell-customer: Abre psql no banco do Customer
db-shell-customer:
	docker exec -it customers-db psql -U postgres -d customers_db

## db-shell-product: Abre psql no banco do Product
db-shell-product:
	docker exec -it products-db psql -U postgres -d products_db

## db-shell-order: Abre psql no banco do Order
db-shell-order:
	docker exec -it orders-db psql -U postgres -d orders_db

## redis-cli: Abre redis-cli
redis-cli:
	docker exec -it redis redis-cli

# ════════════════════════════════════════════════════════════════════════════
# TESTES
# ════════════════════════════════════════════════════════════════════════════

## test: Roda todos os testes
test: test-unit test-api
	@echo "$(GREEN)✔ Suite completa de testes concluída$(NC)"

## test-unit: Testes unitários e de aplicação (.NET)
test-unit:
	@echo "$(CYAN)▶ Rodando testes unitários...$(NC)"
	$(DOTNET) test tests/Customer.Domain.Tests/Customer.Domain.Tests.csproj      --logger "console;verbosity=minimal" --no-restore
	$(DOTNET) test tests/Customer.Application.Tests/Customer.Application.Tests.csproj --logger "console;verbosity=minimal" --no-restore
	$(DOTNET) test tests/Product.Domain.Tests/Product.Domain.Tests.csproj        --logger "console;verbosity=minimal" --no-restore
	$(DOTNET) test tests/Order.Domain.Tests/Order.Domain.Tests.csproj            --logger "console;verbosity=minimal" --no-restore
	$(DOTNET) test tests/Order.Application.Tests/Order.Application.Tests.csproj  --logger "console;verbosity=minimal" --no-restore
	@echo "$(GREEN)✔ Testes unitários OK$(NC)"

## test-integration: Testes de integração com banco real
test-integration:
	@echo "$(CYAN)▶ Rodando testes de integração...$(NC)"
	@$(MAKE) up > /dev/null 2>&1 || true
	@sleep 10
	$(DOTNET) test tests/ --filter "Category=Integration" --logger "console;verbosity=normal"

## test-api: Testes de API end-to-end
test-api:
	@echo "$(CYAN)▶ Rodando testes de API...$(NC)"
	@bash run-tests.sh docker

## test-watch: Testes em modo watch (hot reload)
test-watch:
	$(DOTNET) watch test --project tests/Customer.Domain.Tests/

## test-coverage: Cobertura de código
test-coverage:
	$(DOTNET) test tests/ \
		--collect:"XPlat Code Coverage" \
		--results-directory ./coverage
	$(DOTNET) tool run reportgenerator \
		-reports:coverage/**/coverage.cobertura.xml \
		-targetdir:coverage/report \
		-reporttypes:Html
	@echo "$(GREEN)✔ Relatório em coverage/report/index.html$(NC)"

# ════════════════════════════════════════════════════════════════════════════
# K6 — LOAD TESTS
# ════════════════════════════════════════════════════════════════════════════

## k6-smoke: Smoke test — 1 VU, 1 min (sanidade básica)
k6-smoke:
	@echo "$(CYAN)▶ K6 Smoke Test...$(NC)"
	@mkdir -p $(RESULTS_DIR)
	$(K6) run k6/scripts/smoke.js \
		--env CUSTOMER_URL=$(CUSTOMER_URL) \
		--env PRODUCT_URL=$(PRODUCT_URL) \
		--env ORDER_URL=$(ORDER_URL) \
		--out json=$(RESULTS_DIR)/smoke-$(shell date +%Y%m%d-%H%M%S).json

## k6-load: Load test — 50 VUs, 5 min (carga normal)
k6-load:
	@echo "$(CYAN)▶ K6 Load Test (50 VUs / 5min)...$(NC)"
	@mkdir -p $(RESULTS_DIR)
	$(K6) run k6/scripts/load.js \
		--env CUSTOMER_URL=$(CUSTOMER_URL) \
		--env PRODUCT_URL=$(PRODUCT_URL) \
		--env ORDER_URL=$(ORDER_URL) \
		--out json=$(RESULTS_DIR)/load-$(shell date +%Y%m%d-%H%M%S).json

## k6-stress: Stress test — até 200 VUs (limite do sistema)
k6-stress:
	@echo "$(CYAN)▶ K6 Stress Test (até 200 VUs)...$(NC)"
	@mkdir -p $(RESULTS_DIR)
	$(K6) run k6/scripts/stress.js \
		--env CUSTOMER_URL=$(CUSTOMER_URL) \
		--env PRODUCT_URL=$(PRODUCT_URL) \
		--env ORDER_URL=$(ORDER_URL) \
		--out json=$(RESULTS_DIR)/stress-$(shell date +%Y%m%d-%H%M%S).json

## k6-spike: Spike test — pico repentino de tráfego
k6-spike:
	@echo "$(CYAN)▶ K6 Spike Test...$(NC)"
	@mkdir -p $(RESULTS_DIR)
	$(K6) run k6/scripts/spike.js \
		--env CUSTOMER_URL=$(CUSTOMER_URL) \
		--env PRODUCT_URL=$(PRODUCT_URL) \
		--env ORDER_URL=$(ORDER_URL) \
		--out json=$(RESULTS_DIR)/spike-$(shell date +%Y%m%d-%H%M%S).json

## k6-soak: Soak test — 30 VUs por 30 minutos (resistência)
k6-soak:
	@echo "$(YELLOW)⚠ Soak test: 30 VUs por 30 minutos$(NC)"
	@mkdir -p $(RESULTS_DIR)
	$(K6) run k6/scripts/soak.js \
		--env CUSTOMER_URL=$(CUSTOMER_URL) \
		--env PRODUCT_URL=$(PRODUCT_URL) \
		--env ORDER_URL=$(ORDER_URL) \
		--out json=$(RESULTS_DIR)/soak-$(shell date +%Y%m%d-%H%M%S).json

## k6-all: Roda smoke → load → stress em sequência
k6-all: k6-smoke k6-load k6-stress
	@echo "$(GREEN)✔ Suite K6 completa$(NC)"

## k6-dashboard: Sobe InfluxDB + Grafana para visualizar k6 em tempo real
k6-dashboard:
	$(COMPOSE) -f k6/docker-compose.k6.yml up -d
	@echo "$(GREEN)Grafana K6: http://localhost:3001$(NC)"

## k6-report: Abre último relatório HTML
k6-report:
	@open $(RESULTS_DIR)/$(shell ls -t $(RESULTS_DIR)/*.json 2>/dev/null | head -1) 2>/dev/null || \
		echo "Nenhum resultado encontrado em $(RESULTS_DIR)/"

# ════════════════════════════════════════════════════════════════════════════
# KUBERNETES
# ════════════════════════════════════════════════════════════════════════════

## k8s-up: Aplica todos os manifests no cluster
k8s-up:
	@echo "$(CYAN)▶ Aplicando manifests k8s...$(NC)"
	$(KUBECTL) apply -f k8s/namespace.yaml
	$(KUBECTL) apply -f k8s/infra.yaml
	@echo "Aguardando infraestrutura k8s..."
	@sleep 20
	$(KUBECTL) apply -f k8s/secrets/
	$(KUBECTL) apply -f k8s/configmaps/
	$(KUBECTL) apply -f k8s/deployments/
	$(KUBECTL) apply -f k8s/hpa/
	$(KUBECTL) apply -f k8s/ingress/
	@$(MAKE) k8s-status

## k8s-down: Remove namespace ecommerce
k8s-down:
	$(KUBECTL) delete namespace $(NAMESPACE) --ignore-not-found

## k8s-status: Status dos pods
k8s-status:
	$(KUBECTL) get pods,svc,hpa -n $(NAMESPACE)

## k8s-logs SVC=customer-service: Logs de um pod
k8s-logs:
	$(KUBECTL) logs -n $(NAMESPACE) -l app=$(SVC) -f --tail=100

## k8s-test: Testes via port-forward automático
k8s-test:
	@bash run-tests.sh k8s

## k8s-rollout SVC=customer-service: Força restart de um deployment
k8s-rollout:
	$(KUBECTL) rollout restart deployment/$(SVC) -n $(NAMESPACE)
	$(KUBECTL) rollout status deployment/$(SVC) -n $(NAMESPACE)

## k8s-describe SVC=customer-service: Descreve um pod
k8s-describe:
	$(KUBECTL) describe pod -l app=$(SVC) -n $(NAMESPACE)

# ════════════════════════════════════════════════════════════════════════════
# OBSERVABILIDADE
# ════════════════════════════════════════════════════════════════════════════

## health-all: Checa /health de todos os serviços
health-all:
	@echo "$(CYAN)▶ Health checks...$(NC)"
	@for svc in "customer-service:5001" "product-service:5002" "order-service:5003" "notification-service:5004"; do \
		name=$$(echo $$svc | cut -d: -f1); \
		port=$$(echo $$svc | cut -d: -f2); \
		if curl -sf http://localhost:$$port/health > /dev/null 2>&1; then \
			echo "  $(GREEN)✔$(NC) $$name (http://localhost:$$port)"; \
		else \
			echo "  $(YELLOW)✗$(NC) $$name não respondeu"; \
		fi; \
	done

## outbox-status: Mostra mensagens pendentes/deadletter no Outbox
outbox-status:
	@echo "$(CYAN)▶ Outbox Status — Customer$(NC)"
	@docker exec customers-db psql -U postgres -d customers_db -c \
		"SELECT status, COUNT(*) as total, MAX(retry_count) as max_retries FROM \"OutboxMessages\" GROUP BY status;" 2>/dev/null || echo "  (banco não está rodando)"
	@echo "$(CYAN)▶ Outbox Status — Product$(NC)"
	@docker exec products-db psql -U postgres -d products_db -c \
		"SELECT status, COUNT(*) as total, MAX(retry_count) as max_retries FROM \"OutboxMessages\" GROUP BY status;" 2>/dev/null || echo "  (banco não está rodando)"
	@echo "$(CYAN)▶ Outbox Status — Order$(NC)"
	@docker exec orders-db psql -U postgres -d orders_db -c \
		"SELECT status, COUNT(*) as total, MAX(retry_count) as max_retries FROM \"OutboxMessages\" GROUP BY status;" 2>/dev/null || echo "  (banco não está rodando)"

## deadletter-requeue: Reprocessa mensagens DeadLetter (reseta para Pending)
deadletter-requeue:
	@echo "$(YELLOW)⚠ Refilando mensagens DeadLetter...$(NC)"
	@for db in "customers-db:customers_db" "products-db:products_db" "orders-db:orders_db"; do \
		container=$$(echo $$db | cut -d: -f1); \
		dbname=$$(echo $$db | cut -d: -f2); \
		docker exec $$container psql -U postgres -d $$dbname -c \
			"UPDATE \"OutboxMessages\" SET status='Pending', retry_count=0, is_processing=false, error=NULL WHERE status='DeadLetter';" 2>/dev/null || true; \
	done
	@echo "$(GREEN)✔ Mensagens refiladas$(NC)"

## rabbit-ui: Abre RabbitMQ Management
rabbit-ui:
	@open http://localhost:15672 2>/dev/null || xdg-open http://localhost:15672

## jaeger: Abre Jaeger UI
jaeger:
	@open http://localhost:16686 2>/dev/null || xdg-open http://localhost:16686

## prometheus: Abre Prometheus
prometheus:
	@open http://localhost:9090 2>/dev/null || xdg-open http://localhost:9090

## grafana: Abre Grafana
grafana:
	@open http://localhost:3000 2>/dev/null || xdg-open http://localhost:3000

# ════════════════════════════════════════════════════════════════════════════
# CÓDIGO
# ════════════════════════════════════════════════════════════════════════════

## restore: Restaura pacotes NuGet
restore:
	$(DOTNET) restore ECommerce.sln

## build-dotnet: Build da solution
build-dotnet:
	$(DOTNET) build ECommerce.sln --no-restore -c Release

## lint: Roda analyzers
lint:
	$(DOTNET) build ECommerce.sln -warnaserror --no-restore

## format: Formata o código
format:
	$(DOTNET) format ECommerce.sln

## clean: Limpa bin/obj
clean:
	find . -name "bin" -type d | grep -v ".git" | xargs rm -rf
	find . -name "obj" -type d | grep -v ".git" | xargs rm -rf
	@echo "$(GREEN)✔ Limpo$(NC)"
