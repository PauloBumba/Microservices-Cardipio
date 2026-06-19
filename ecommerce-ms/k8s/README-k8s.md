# Como testar as APIs

## Opção 1 — Docker Compose (mais fácil, recomendado)

```bash
# Roda tudo: build, sobe infra + serviços, testa, derruba
bash run-tests.sh docker

# Manter containers rodando após os testes (para inspecionar)
KEEP=1 bash run-tests.sh docker
```

## Opção 2 — Kubernetes (minikube / kind / k3d)

### Pré-requisitos
```bash
# Instalar kind (ou minikube/k3d)
brew install kind        # macOS
choco install kind       # Windows
curl -Lo kind ... && install  # Linux

# Criar cluster local
kind create cluster --name ecommerce
```

### Subir tudo no k8s
```bash
# 1. Namespace
kubectl apply -f k8s/namespace.yaml

# 2. Infraestrutura (banco, redis, rabbitmq)
kubectl apply -f k8s/infra.yaml

# Aguardar infra
kubectl wait --for=condition=ready pod -l app=rabbitmq -n ecommerce --timeout=120s

# 3. Secrets e ConfigMaps
kubectl apply -f k8s/secrets/
kubectl apply -f k8s/configmaps/

# 4. Carregar imagens no kind (sem registry externo)
docker build -f src/Customer/Api/Dockerfile -t ecommerce/customer-service:latest .
docker build -f src/Product/Api/Dockerfile  -t ecommerce/product-service:latest  .
docker build -f src/Order/Api/Dockerfile    -t ecommerce/order-service:latest    .
docker build -f src/Notification/Api/Dockerfile -t ecommerce/notification-service:latest .

kind load docker-image ecommerce/customer-service:latest     --name ecommerce
kind load docker-image ecommerce/product-service:latest      --name ecommerce
kind load docker-image ecommerce/order-service:latest        --name ecommerce
kind load docker-image ecommerce/notification-service:latest --name ecommerce

# 5. Deployments
kubectl apply -f k8s/deployments/

# Aguardar pods
kubectl wait --for=condition=ready pod -l app=customer-service    -n ecommerce --timeout=120s
kubectl wait --for=condition=ready pod -l app=product-service     -n ecommerce --timeout=120s
kubectl wait --for=condition=ready pod -l app=order-service       -n ecommerce --timeout=120s
kubectl wait --for=condition=ready pod -l app=notification-service -n ecommerce --timeout=120s

# 6. HPA e Ingress (opcional)
kubectl apply -f k8s/hpa/
kubectl apply -f k8s/ingress/
```

### Rodar os testes via k8s
```bash
bash run-tests.sh k8s
```

### Ou manualmente com port-forward
```bash
kubectl port-forward -n ecommerce svc/customer-service    5001:8080 &
kubectl port-forward -n ecommerce svc/product-service     5002:8080 &
kubectl port-forward -n ecommerce svc/order-service       5003:8080 &
kubectl port-forward -n ecommerce svc/notification-service 5004:8080 &

bash test-apis.sh
```

### Comandos úteis
```bash
# Ver todos os pods
kubectl get pods -n ecommerce

# Ver logs de um serviço
kubectl logs -n ecommerce -l app=customer-service -f

# Ver logs do OutboxProcessor
kubectl logs -n ecommerce -l app=customer-service -f | grep "\[Outbox\]"

# Ver eventos do cluster
kubectl get events -n ecommerce --sort-by=.lastTimestamp

# Ver HPA em ação
kubectl get hpa -n ecommerce -w

# Deletar tudo
kubectl delete namespace ecommerce
kind delete cluster --name ecommerce
```

## O que os testes validam

| Categoria | Testes |
|---|---|
| Health Checks | Todos os 4 serviços respondem /health |
| Customer CRUD | Criar, listar, buscar por ID, atualizar, desativar |
| Validação | Email duplicado, campos obrigatórios (FluentValidation) |
| Cache | Segunda chamada GET vem do Redis |
| Product CRUD | Criar, listar, buscar, adicionar estoque |
| Order Flow | Criar → Confirmar → Cancelar (regras de negócio) |
| Outbox | Eventos gerados e processados assincronamente |
| Idempotência | Ações duplicadas retornam 400 corretamente |
| ApiResponse | Todos os endpoints retornam `{isSuccess, data, errors}` |
