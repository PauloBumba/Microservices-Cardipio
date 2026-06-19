#!/bin/bash
# Popula dados de teste para facilitar desenvolvimento e debugging

CUSTOMER_URL="${CUSTOMER_URL:-http://localhost:5001}"
PRODUCT_URL="${PRODUCT_URL:-http://localhost:5002}"
ORDER_URL="${ORDER_URL:-http://localhost:5003}"

echo "▶ Seed: criando customers..."
C1=$(curl -s -X POST "$CUSTOMER_URL/api/customers" \
  -H "Content-Type: application/json" \
  -d '{"name":"Ana Silva","email":"ana@seed.com","phone":"11999990001","street":"Rua A, 1","city":"São Paulo","state":"SP","zipCode":"01310-100","country":"Brasil"}' \
  | grep -o '"data":"[^"]*"' | cut -d'"' -f4)

C2=$(curl -s -X POST "$CUSTOMER_URL/api/customers" \
  -H "Content-Type: application/json" \
  -d '{"name":"Bruno Lima","email":"bruno@seed.com","phone":"11999990002","street":"Rua B, 2","city":"Campinas","state":"SP","zipCode":"13010-001","country":"Brasil"}' \
  | grep -o '"data":"[^"]*"' | cut -d'"' -f4)

echo "  Customer 1: $C1"
echo "  Customer 2: $C2"

echo "▶ Seed: criando products..."
P1=$(curl -s -X POST "$PRODUCT_URL/api/products" \
  -H "Content-Type: application/json" \
  -d '{"name":"Notebook Pro X","description":"Notebook para desenvolvedores","sku":"NB-PRO-X","price":5999.99,"currency":"BRL","initialStock":100,"category":"Eletrônicos"}' \
  | grep -o '"data":"[^"]*"' | cut -d'"' -f4)

P2=$(curl -s -X POST "$PRODUCT_URL/api/products" \
  -H "Content-Type: application/json" \
  -d '{"name":"Mouse Gamer RGB","description":"Mouse de alta precisão","sku":"MG-RGB-001","price":299.99,"currency":"BRL","initialStock":500,"category":"Periféricos"}' \
  | grep -o '"data":"[^"]*"' | cut -d'"' -f4)

P3=$(curl -s -X POST "$PRODUCT_URL/api/products" \
  -H "Content-Type: application/json" \
  -d '{"name":"Teclado Mecânico","description":"Switch blue","sku":"TM-BLUE-001","price":449.90,"currency":"BRL","initialStock":200,"category":"Periféricos"}' \
  | grep -o '"data":"[^"]*"' | cut -d'"' -f4)

echo "  Product 1: $P1"
echo "  Product 2: $P2"
echo "  Product 3: $P3"

echo "▶ Seed: criando orders..."
if [ -n "$C1" ] && [ -n "$P1" ] && [ -n "$P2" ]; then
  O1=$(curl -s -X POST "$ORDER_URL/api/orders" \
    -H "Content-Type: application/json" \
    -d "{\"customerId\":\"$C1\",\"items\":[{\"productId\":\"$P1\",\"productName\":\"Notebook Pro X\",\"sku\":\"NB-PRO-X\",\"quantity\":1,\"unitPrice\":5999.99,\"currency\":\"BRL\"},{\"productId\":\"$P2\",\"productName\":\"Mouse Gamer RGB\",\"sku\":\"MG-RGB-001\",\"quantity\":2,\"unitPrice\":299.99,\"currency\":\"BRL\"}]}" \
    | grep -o '"data":"[^"]*"' | cut -d'"' -f4)
  echo "  Order 1: $O1"

  # Confirma o pedido
  if [ -n "$O1" ]; then
    curl -s -X PUT "$ORDER_URL/api/orders/$O1/confirm" > /dev/null
    echo "  Order 1 confirmado"
  fi
fi

echo ""
echo "✔ Seed concluído!"
echo "  Acesse Adminer: http://localhost:8080"
echo "  RabbitMQ UI:    http://localhost:15672"
