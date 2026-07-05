# ════════════════════════════════════════════════════════════════════════════
# KUBERNETES (kind)
# ════════════════════════════════════════════════════════════════════════════
#
# Fluxo completo do zero:
#   make k8s-cluster-up      # cria o cluster kind + instala ingress-nginx
#   make k8s-build           # builda as 4 imagens e carrega no cluster
#   make k8s-up              # aplica namespace, infra, secrets, deployments, hpa, ingress
#   make k8s-test            # roda os testes via port-forward
#   make k8s-down             # remove só o namespace (mantém o cluster)
#   make k8s-cluster-down     # destrói o cluster kind inteiro

KIND            := kind
KIND_CLUSTER    := ecommerce
IMAGE_TAG       ?= latest
IMAGE_REGISTRY  := ecommerce

## k8s-cluster-up: Cria o cluster kind (idempotente) e instala o ingress-nginx
k8s-cluster-up:
	@if $(KIND) get clusters 2>/dev/null | grep -qx "$(KIND_CLUSTER)"; then \
		echo "$(YELLOW)Cluster '$(KIND_CLUSTER)' já existe, pulando criação.$(NC)"; \
	else \
		echo "$(CYAN)▶ Criando cluster kind '$(KIND_CLUSTER)'...$(NC)"; \
		$(KIND) create cluster --name $(KIND_CLUSTER) --config k8s/kind-config.yaml; \
	fi
	@$(MAKE) k8s-ingress-install

## k8s-cluster-down: Destrói o cluster kind inteiro (todos os dados vão junto)
k8s-cluster-down:
	$(KIND) delete cluster --name $(KIND_CLUSTER)

## k8s-ingress-install: Instala o controller ingress-nginx (build oficial p/ kind) e aguarda ficar pronto
k8s-ingress-install:
	@echo "$(CYAN)▶ Instalando ingress-nginx...$(NC)"
	$(KUBECTL) apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml
	@echo "Aguardando ingress-nginx controller ficar pronto..."
	$(KUBECTL) wait --namespace ingress-nginx \
		--for=condition=ready pod \
		--selector=app.kubernetes.io/component=controller \
		--timeout=120s

## k8s-build: Builda as 4 imagens dos serviços e carrega no cluster kind (sem registry externa)
k8s-build:
	@echo "$(CYAN)▶ Buildando imagens...$(NC)"
	docker build -f src/Customer/Api/Dockerfile     -t $(IMAGE_REGISTRY)/customer-service:$(IMAGE_TAG)     .
	docker build -f src/Product/Api/Dockerfile      -t $(IMAGE_REGISTRY)/product-service:$(IMAGE_TAG)      .
	docker build -f src/Order/Api/Dockerfile        -t $(IMAGE_REGISTRY)/order-service:$(IMAGE_TAG)        .
	docker build -f src/Notification/Api/Dockerfile -t $(IMAGE_REGISTRY)/notification-service:$(IMAGE_TAG) .
	@echo "$(CYAN)▶ Carregando imagens no cluster kind...$(NC)"
	$(KIND) load docker-image $(IMAGE_REGISTRY)/customer-service:$(IMAGE_TAG)     --name $(KIND_CLUSTER)
	$(KIND) load docker-image $(IMAGE_REGISTRY)/product-service:$(IMAGE_TAG)      --name $(KIND_CLUSTER)
	$(KIND) load docker-image $(IMAGE_REGISTRY)/order-service:$(IMAGE_TAG)        --name $(KIND_CLUSTER)
	$(KIND) load docker-image $(IMAGE_REGISTRY)/notification-service:$(IMAGE_TAG) --name $(KIND_CLUSTER)

## k8s-up: Aplica todos os manifests no cluster
k8s-up:
	@echo "$(CYAN)▶ Aplicando manifests k8s...$(NC)"
	$(KUBECTL) apply -f k8s/namespace.yaml
	$(KUBECTL) apply -f k8s/infra.yaml
	@echo "Aguardando infraestrutura k8s..."
	$(KUBECTL) wait --for=condition=ready pod -l app=rabbitmq -n $(NAMESPACE) --timeout=120s
	$(KUBECTL) wait --for=condition=ready pod -l app=redis    -n $(NAMESPACE) --timeout=120s
	$(KUBECTL) apply -f k8s/secrets/
	$(KUBECTL) apply -f k8s/configmaps/
	$(KUBECTL) apply -f k8s/deployments/
	@echo "Aguardando os serviços ficarem prontos..."
	$(KUBECTL) wait --for=condition=available deployment -l app -n $(NAMESPACE) --timeout=180s || true
	$(KUBECTL) apply -f k8s/hpa/
	$(KUBECTL) apply -f k8s/ingress/
	@$(MAKE) k8s-status

## k8s-all: Fluxo completo do zero — cluster + build + manifests, numa tacada só
k8s-all: k8s-cluster-up k8s-build k8s-up

## k8s-down: Remove namespace ecommerce (mantém o cluster kind rodando)
k8s-down:
	$(KUBECTL) delete namespace $(NAMESPACE) --ignore-not-found

## k8s-status: Status dos pods
k8s-status:
	$(KUBECTL) get pods,svc,hpa,ingress -n $(NAMESPACE)

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