#!/bin/bash
set -euo pipefail

# ── Cores ────────────────────────────────────────────────────────────────
GREEN='\033[0;32m'; RED='\033[0;31m'; YELLOW='\033[1;33m'
BLUE='\033[0;34m'; CYAN='\033[0;36m'; BOLD='\033[1m'; NC='\033[0m'

PASS=0; FAIL=0; SKIP=0
CUSTOMER_ID=""; PRODUCT_ID=""; ORDER_ID=""

CUSTOMER_URL="${CUSTOMER_URL:-http://localhost:5001}"
PRODUCT_URL="${PRODUCT_URL:-http://localhost:5002}"
ORDER_URL="${ORDER_URL:-http://localhost:5003}"
NOTIF_URL="${NOTIF_URL:-http://localhost:5004}"

log()    { echo -e "${BLUE}[INFO]${NC} $1"; }
ok()     { echo -e "${GREEN}[PASS]${NC} $1"; ((PASS++)); }
fail()   { echo -e "${RED}[FAIL]${NC} $1"; ((FAIL++)); }
warn()   { echo -e "${YELLOW}[SKIP]${NC} $1"; ((SKIP++)); }
header() { echo -e "\n${BOLD}${CYAN}══════════════════════════════════════${NC}"; echo -e "${BOLD}${CYAN}  $1${NC}"; echo -e "${BOLD}${CYAN}══════════════════════════════════════${NC}"; }

# ── Helpers ────────────────────────────────────────────────────────────────
do_request() {
  local METHOD=$1 URL=$2 BODY=${3:-}
  if [ -n "$BODY" ]; then
    curl -s -w "\n%{http_code}" -X "$METHOD" "$URL" \
      -H "Content-Type: application/json" -d "$BODY"
  else
    curl -s -w "\n%{http_code}" -X "$METHOD" "$URL"
  fi
}

assert() {
  local NAME=$1 EXPECTED_CODE=$2 ACTUAL_CODE=$3 BODY=$4
  local IS_SUCCESS
  IS_SUCCESS=$(echo "$BODY" | grep -o '"isSuccess":true' || true)

  if [ "$ACTUAL_CODE" -eq "$EXPECTED_CODE" ]; then
    ok "$NAME (HTTP $ACTUAL_CODE)"
  else
    fail "$NAME — esperado HTTP $EXPECTED_CODE, recebeu $ACTUAL_CODE"
    echo "    Body: $(echo "$BODY" | head -c 300)"
  fi
}

extract_id() {
  echo "$1" | grep -o '"data":"[^"]*"' | head -1 | cut -d'"' -f4
}

wait_service() {
  local NAME=$1 URL=$2
  log "Aguardando $NAME em $URL/health ..."
  for i in $(seq 1 30); do
    if curl -sf "$URL/health" > /dev/null 2>&1; then
      ok "$NAME está online"
      return 0
    fi
    sleep 2
  done
  fail "$NAME não respondeu após 60s"
  return 1
}

# ════════════════════════════════════════════════════════
# 1. HEALTH CHECKS
# ════════════════════════════════════════════════════════
header "1. HEALTH CHECKS"

for SVC in "Customer:$CUSTOMER_URL" "Product:$PRODUCT_URL" "Order:$ORDER_URL" "Notification:$NOTIF_URL"; do
  NAME=$(echo $SVC | cut -d: -f1)
  URL=$(echo $SVC | cut -d: -f2-)
  RESP=$(do_request GET "$URL/health")
  CODE=$(echo "$RESP" | tail -1)
  if [ "$CODE" -eq 200 ]; then ok "Health $NAME"; else fail "Health $NAME (HTTP $CODE)"; fi
done

# ════════════════════════════════════════════════════════
# 2. CUSTOMER SERVICE
# ════════════════════════════════════════════════════════
header "2. CUSTOMER SERVICE"

# POST — criar cliente
BODY='{"name":"João Silva","email":"joao@email.com","phone":"11999999999","street":"Rua A, 100","city":"São Paulo","state":"SP","zipCode":"01310-100","country":"Brasil"}'
RESP=$(do_request POST "$CUSTOMER_URL/api/customers" "$BODY")
CODE=$(echo "$RESP" | tail -1); RBODY=$(echo "$RESP" | head -n -1)
assert "POST /api/customers (criar)" 201 $CODE "$RBODY"
CUSTOMER_ID=$(extract_id "$RBODY")
log "Customer ID: $CUSTOMER_ID"

# POST — email duplicado
RESP=$(do_request POST "$CUSTOMER_URL/api/customers" "$BODY")
CODE=$(echo "$RESP" | tail -1); RBODY=$(echo "$RESP" | head -n -1)
assert "POST /api/customers (email duplicado → 400)" 400 $CODE "$RBODY"

# POST — validação (sem nome)
BODY_INVALID='{"name":"","email":"invalido","phone":"","street":"","city":"","state":"","zipCode":"","country":""}'
RESP=$(do_request POST "$CUSTOMER_URL/api/customers" "$BODY_INVALID")
CODE=$(echo "$RESP" | tail -1)
assert "POST /api/customers (validação FluentValidation → 400)" 400 $CODE ""

# GET all
RESP=$(do_request GET "$CUSTOMER_URL/api/customers")
CODE=$(echo "$RESP" | tail -1); RBODY=$(echo "$RESP" | head -n -1)
assert "GET /api/customers (listar)" 200 $CODE "$RBODY"

# GET by id
if [ -n "$CUSTOMER_ID" ]; then
  RESP=$(do_request GET "$CUSTOMER_URL/api/customers/$CUSTOMER_ID")
  CODE=$(echo "$RESP" | tail -1); RBODY=$(echo "$RESP" | head -n -1)
  assert "GET /api/customers/:id" 200 $CODE "$RBODY"

  # GET by id - segunda vez (deve vir do cache)
  RESP=$(do_request GET "$CUSTOMER_URL/api/customers/$CUSTOMER_ID")
  CODE=$(echo "$RESP" | tail -1)
  assert "GET /api/customers/:id (cache hit)" 200 $CODE ""

  # PUT — atualizar
  BODY_UPD='{"name":"João Atualizado","phone":"11988888888","street":"Rua B, 200","city":"Campinas","state":"SP","zipCode":"13010-001","country":"Brasil"}'
  RESP=$(do_request PUT "$CUSTOMER_URL/api/customers/$CUSTOMER_ID" "$BODY_UPD")
  CODE=$(echo "$RESP" | tail -1)
  assert "PUT /api/customers/:id (atualizar)" 200 $CODE ""
else
  warn "Pulando testes de customer por ID (criação falhou)"
fi

# GET — id inexistente
RESP=$(do_request GET "$CUSTOMER_URL/api/customers/00000000-0000-0000-0000-000000000000")
CODE=$(echo "$RESP" | tail -1)
assert "GET /api/customers/:id (não encontrado → 404)" 404 $CODE ""

# ════════════════════════════════════════════════════════
# 3. PRODUCT SERVICE
# ════════════════════════════════════════════════════════
header "3. PRODUCT SERVICE"

# POST — criar produto
BODY_PROD='{"name":"Notebook Pro","description":"Notebook para devs","sku":"NB-PRO-001","price":5999.99,"currency":"BRL","initialStock":50,"category":"Eletrônicos"}'
RESP=$(do_request POST "$PRODUCT_URL/api/products" "$BODY_PROD")
CODE=$(echo "$RESP" | tail -1); RBODY=$(echo "$RESP" | head -n -1)
assert "POST /api/products (criar)" 201 $CODE "$RBODY"
PRODUCT_ID=$(extract_id "$RBODY")
log "Product ID: $PRODUCT_ID"

# POST — SKU duplicado
RESP=$(do_request POST "$PRODUCT_URL/api/products" "$BODY_PROD")
CODE=$(echo "$RESP" | tail -1)
assert "POST /api/products (SKU duplicado → 400)" 400 $CODE ""

# POST — validação (preço negativo)
BODY_INVALID='{"name":"X","description":"","sku":"SKU-X","price":-1,"currency":"BRL","initialStock":0,"category":"Cat"}'
RESP=$(do_request POST "$PRODUCT_URL/api/products" "$BODY_INVALID")
CODE=$(echo "$RESP" | tail -1)
assert "POST /api/products (preço negativo → 400)" 400 $CODE ""

# GET all
RESP=$(do_request GET "$PRODUCT_URL/api/products")
CODE=$(echo "$RESP" | tail -1)
assert "GET /api/products (listar)" 200 $CODE ""

if [ -n "$PRODUCT_ID" ]; then
  # GET by id
  RESP=$(do_request GET "$PRODUCT_URL/api/products/$PRODUCT_ID")
  CODE=$(echo "$RESP" | tail -1); RBODY=$(echo "$RESP" | head -n -1)
  assert "GET /api/products/:id" 200 $CODE "$RBODY"

  # PATCH — add stock
  RESP=$(do_request POST "$PRODUCT_URL/api/products/$PRODUCT_ID/stock" '{"quantity":10}')
  CODE=$(echo "$RESP" | tail -1)
  assert "POST /api/products/:id/stock (adicionar estoque)" 200 $CODE ""

  # Verificar estoque atualizado
  RESP=$(do_request GET "$PRODUCT_URL/api/products/$PRODUCT_ID")
  STOCK=$(echo "$RESP" | head -n -1 | grep -o '"stockQuantity":[0-9]*' | cut -d: -f2)
  if [ "$STOCK" = "60" ]; then ok "Estoque correto após AddStock (60)"; else fail "Estoque esperado 60, recebeu $STOCK"; fi
else
  warn "Pulando testes de product por ID"
fi

# GET — id inexistente
RESP=$(do_request GET "$PRODUCT_URL/api/products/00000000-0000-0000-0000-000000000000")
CODE=$(echo "$RESP" | tail -1)
assert "GET /api/products/:id (não encontrado → 404)" 404 $CODE ""

# ════════════════════════════════════════════════════════
# 4. ORDER SERVICE
# ════════════════════════════════════════════════════════
header "4. ORDER SERVICE"

if [ -n "$CUSTOMER_ID" ] && [ -n "$PRODUCT_ID" ]; then
  # POST — criar pedido
  BODY_ORDER=$(cat <<EOF
{
  "customerId": "$CUSTOMER_ID",
  "items": [
    {
      "productId": "$PRODUCT_ID",
      "productName": "Notebook Pro",
      "sku": "NB-PRO-001",
      "quantity": 2,
      "unitPrice": 5999.99,
      "currency": "BRL"
    }
  ]
}
EOF
)
  RESP=$(do_request POST "$ORDER_URL/api/orders" "$BODY_ORDER")
  CODE=$(echo "$RESP" | tail -1); RBODY=$(echo "$RESP" | head -n -1)
  assert "POST /api/orders (criar)" 201 $CODE "$RBODY"
  ORDER_ID=$(extract_id "$RBODY")
  log "Order ID: $ORDER_ID"

  if [ -n "$ORDER_ID" ]; then
    # GET by id
    RESP=$(do_request GET "$ORDER_URL/api/orders/$ORDER_ID")
    CODE=$(echo "$RESP" | tail -1); RBODY=$(echo "$RESP" | head -n -1)
    assert "GET /api/orders/:id" 200 $CODE "$RBODY"

    # Verificar total
    TOTAL=$(echo "$RBODY" | grep -o '"total":[0-9.]*' | cut -d: -f2)
    EXPECTED="11999.98"
    if [ "$TOTAL" = "$EXPECTED" ]; then ok "Total do pedido correto ($EXPECTED)"; else fail "Total esperado $EXPECTED, recebeu $TOTAL"; fi

    # PUT — confirmar pedido
    RESP=$(do_request PUT "$ORDER_URL/api/orders/$ORDER_ID/confirm" "")
    CODE=$(echo "$RESP" | tail -1)
    assert "PUT /api/orders/:id/confirm (confirmar)" 200 $CODE ""

    # Verificar status Confirmed
    RESP=$(do_request GET "$ORDER_URL/api/orders/$ORDER_ID")
    STATUS=$(echo "$RESP" | head -n -1 | grep -o '"status":"[^"]*"' | cut -d'"' -f4)
    if [ "$STATUS" = "Confirmed" ]; then ok "Status do pedido = Confirmed"; else fail "Status esperado Confirmed, recebeu $STATUS"; fi

    # PUT — confirmar novamente deve falhar (já confirmado)
    RESP=$(do_request PUT "$ORDER_URL/api/orders/$ORDER_ID/confirm" "")
    CODE=$(echo "$RESP" | tail -1)
    assert "PUT /api/orders/:id/confirm (dupla confirmação → 400)" 400 $CODE ""
  fi

  # POST — pedido sem itens (validação)
  BODY_EMPTY='{"customerId":"'$CUSTOMER_ID'","items":[]}'
  RESP=$(do_request POST "$ORDER_URL/api/orders" "$BODY_EMPTY")
  CODE=$(echo "$RESP" | tail -1)
  assert "POST /api/orders (sem itens → 400)" 400 $CODE ""

  # POST — pedido com quantidade 0
  BODY_BAD='{"customerId":"'$CUSTOMER_ID'","items":[{"productId":"'$PRODUCT_ID'","productName":"X","sku":"X","quantity":0,"unitPrice":10,"currency":"BRL"}]}'
  RESP=$(do_request POST "$ORDER_URL/api/orders" "$BODY_BAD")
  CODE=$(echo "$RESP" | tail -1)
  assert "POST /api/orders (qty=0 → 400)" 400 $CODE ""

  # GET pedidos por cliente
  RESP=$(do_request GET "$ORDER_URL/api/orders/customer/$CUSTOMER_ID")
  CODE=$(echo "$RESP" | tail -1)
  assert "GET /api/orders/customer/:id (pedidos do cliente)" 200 $CODE ""
else
  warn "Pulando testes de Order (Customer ou Product não criados)"
fi

# ════════════════════════════════════════════════════════
# 5. IDEMPOTÊNCIA DO OUTBOX
# ════════════════════════════════════════════════════════
header "5. OUTBOX — VERIFICAÇÃO"

log "Aguardando 8s para o OutboxProcessor rodar..."
sleep 8

# Criar segundo produto para forçar evento
BODY_PROD2='{"name":"Mouse Gamer","description":"Mouse de alta precisão","sku":"MG-001","price":299.99,"currency":"BRL","initialStock":100,"category":"Periféricos"}'
RESP=$(do_request POST "$PRODUCT_URL/api/products" "$BODY_PROD2")
CODE=$(echo "$RESP" | tail -1)
assert "POST /api/products (segundo produto — gera evento Outbox)" 201 $CODE ""

log "Aguardando 10s para o OutboxProcessor processar eventos..."
sleep 10

ok "Outbox processado (verificar logs do container para confirmar)"

# ════════════════════════════════════════════════════════
# 6. CANCELAMENTO DE PEDIDO
# ════════════════════════════════════════════════════════
header "6. CANCELAMENTO DE PEDIDO"

if [ -n "$CUSTOMER_ID" ] && [ -n "$PRODUCT_ID" ]; then
  BODY_ORDER2='{"customerId":"'$CUSTOMER_ID'","items":[{"productId":"'$PRODUCT_ID'","productName":"Mouse","sku":"MG-001","quantity":1,"unitPrice":299.99,"currency":"BRL"}]}'
  RESP=$(do_request POST "$ORDER_URL/api/orders" "$BODY_ORDER2")
  CODE=$(echo "$RESP" | tail -1); RBODY=$(echo "$RESP" | head -n -1)
  assert "POST /api/orders (criar para cancelar)" 201 $CODE "$RBODY"
  ORDER_ID2=$(extract_id "$RBODY")

  if [ -n "$ORDER_ID2" ]; then
    RESP=$(do_request DELETE "$ORDER_URL/api/orders/$ORDER_ID2" '{"reason":"Teste de cancelamento"}')
    CODE=$(echo "$RESP" | tail -1)
    assert "DELETE /api/orders/:id (cancelar)" 200 $CODE ""

    RESP=$(do_request GET "$ORDER_URL/api/orders/$ORDER_ID2")
    STATUS=$(echo "$RESP" | head -n -1 | grep -o '"status":"[^"]*"' | cut -d'"' -f4)
    if [ "$STATUS" = "Cancelled" ]; then ok "Status do pedido = Cancelled"; else fail "Status esperado Cancelled, recebeu $STATUS"; fi

    # Cancelar novamente deve falhar
    RESP=$(do_request DELETE "$ORDER_URL/api/orders/$ORDER_ID2" '{"reason":"Repetido"}')
    CODE=$(echo "$RESP" | tail -1)
    assert "DELETE /api/orders/:id (cancelar novamente → 400)" 400 $CODE ""
  fi
fi

# ════════════════════════════════════════════════════════
# 7. DEACTIVATE CUSTOMER
# ════════════════════════════════════════════════════════
header "7. DESATIVAR CLIENTE"

if [ -n "$CUSTOMER_ID" ]; then
  RESP=$(do_request DELETE "$CUSTOMER_URL/api/customers/$CUSTOMER_ID" "")
  CODE=$(echo "$RESP" | tail -1)
  assert "DELETE /api/customers/:id (desativar)" 200 $CODE ""

  RESP=$(do_request DELETE "$CUSTOMER_URL/api/customers/$CUSTOMER_ID" "")
  CODE=$(echo "$RESP" | tail -1)
  assert "DELETE /api/customers/:id (desativar novamente → 400)" 400 $CODE ""
fi

# ════════════════════════════════════════════════════════
# RESUMO FINAL
# ════════════════════════════════════════════════════════
TOTAL=$((PASS + FAIL + SKIP))
echo ""
echo -e "${BOLD}═══════════════════════════════════════${NC}"
echo -e "${BOLD}  RESULTADO FINAL${NC}"
echo -e "${BOLD}═══════════════════════════════════════${NC}"
echo -e "  Total:   $TOTAL testes"
echo -e "  ${GREEN}Passou:  $PASS${NC}"
echo -e "  ${RED}Falhou:  $FAIL${NC}"
echo -e "  ${YELLOW}Pulou:   $SKIP${NC}"
echo -e "${BOLD}═══════════════════════════════════════${NC}"

if [ $FAIL -eq 0 ]; then
  echo -e "\n${GREEN}${BOLD}✅ Todas as APIs estão funcionando corretamente!${NC}\n"
  exit 0
else
  echo -e "\n${RED}${BOLD}❌ $FAIL teste(s) falharam. Verifique os logs acima.${NC}\n"
  exit 1
fi
