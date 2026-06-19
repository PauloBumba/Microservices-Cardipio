#!/bin/bash
set -euo pipefail

GREEN='\033[0;32m'; RED='\033[0;31m'; YELLOW='\033[1;33m'; BOLD='\033[1m'; NC='\033[0m'

echo -e "${BOLD}╔═══════════════════════════════════════════╗${NC}"
echo -e "${BOLD}║     ecommerce-ms — Test Runner            ║${NC}"
echo -e "${BOLD}╚═══════════════════════════════════════════╝${NC}"

MODE="${1:-docker}"

if [ "$MODE" = "docker" ]; then
  echo -e "\n${YELLOW}[1/4] Buildando imagens...${NC}"
  docker compose build --parallel 2>&1 | grep -E "^#|DONE|ERROR|=>|error" || true

  echo -e "\n${YELLOW}[2/4] Subindo infraestrutura...${NC}"
  docker compose up -d customers-db products-db orders-db notifications-db redis rabbitmq
  echo "Aguardando healthchecks..."
  sleep 15

  echo -e "\n${YELLOW}[3/4] Subindo microserviços...${NC}"
  docker compose up -d customer-service product-service order-service notification-service

  echo -e "\nAguardando serviços inicializarem (migrations + DI)..."
  sleep 20

  echo -e "\n${YELLOW}[4/4] Rodando testes...${NC}\n"
  bash test-apis.sh
  TEST_RESULT=$?

  if [ "${KEEP:-0}" != "1" ]; then
    echo -e "\n${YELLOW}Derrubando containers...${NC}"
    docker compose down -v
  fi

  exit $TEST_RESULT

elif [ "$MODE" = "k8s" ]; then
  echo -e "\n${YELLOW}Modo Kubernetes${NC}"

  # Verificar cluster
  if ! kubectl cluster-info > /dev/null 2>&1; then
    echo -e "${RED}Nenhum cluster Kubernetes encontrado.${NC}"
    echo "Para testar localmente, instale: minikube, kind ou k3d"
    echo ""
    echo "Exemplo com kind:"
    echo "  kind create cluster --name ecommerce"
    echo "  kubectl apply -f k8s/"
    echo "  kubectl port-forward -n ecommerce svc/customer-service 5001:8080 &"
    echo "  kubectl port-forward -n ecommerce svc/product-service 5002:8080 &"
    echo "  kubectl port-forward -n ecommerce svc/order-service 5003:8080 &"
    echo "  kubectl port-forward -n ecommerce svc/notification-service 5004:8080 &"
    echo "  bash test-apis.sh"
    exit 1
  fi

  echo "Aplicando manifests..."
  kubectl apply -f k8s/namespace.yaml
  kubectl apply -f k8s/secrets/
  kubectl apply -f k8s/configmaps/
  kubectl apply -f k8s/deployments/
  kubectl apply -f k8s/ingress/
  kubectl apply -f k8s/hpa/

  echo "Aguardando pods ficarem prontos..."
  kubectl wait --for=condition=ready pod -l app=customer-service -n ecommerce --timeout=120s
  kubectl wait --for=condition=ready pod -l app=product-service -n ecommerce --timeout=120s
  kubectl wait --for=condition=ready pod -l app=order-service -n ecommerce --timeout=120s
  kubectl wait --for=condition=ready pod -l app=notification-service -n ecommerce --timeout=120s

  # Port-forward
  echo "Configurando port-forwards..."
  kubectl port-forward -n ecommerce svc/customer-service 5001:8080 > /dev/null 2>&1 &
  kubectl port-forward -n ecommerce svc/product-service 5002:8080 > /dev/null 2>&1 &
  kubectl port-forward -n ecommerce svc/order-service 5003:8080 > /dev/null 2>&1 &
  kubectl port-forward -n ecommerce svc/notification-service 5004:8080 > /dev/null 2>&1 &
  sleep 5

  bash test-apis.sh
  TEST_RESULT=$?

  # Matar port-forwards
  kill $(jobs -p) 2>/dev/null || true
  exit $TEST_RESULT

elif [ "$MODE" = "local" ]; then
  echo -e "${YELLOW}Modo local (serviços já rodando nas portas padrão)${NC}\n"
  bash test-apis.sh
fi
