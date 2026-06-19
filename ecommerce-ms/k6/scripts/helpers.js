import http from 'k6/http';
import { check } from 'k6';

export const CUSTOMER_URL = __ENV.CUSTOMER_URL || 'http://localhost:5001';
export const PRODUCT_URL  = __ENV.PRODUCT_URL  || 'http://localhost:5002';
export const ORDER_URL    = __ENV.ORDER_URL    || 'http://localhost:5003';

const HEADERS = { 'Content-Type': 'application/json' };

// ── Thresholds padrão reutilizáveis ──────────────────────────────────────
export const DEFAULT_THRESHOLDS = {
  http_req_failed:   ['rate<0.01'],         // < 1% de erros
  http_req_duration: ['p(95)<500'],         // 95% das req < 500ms
  http_req_duration: ['p(99)<1500'],        // 99% < 1500ms
};

export const STRICT_THRESHOLDS = {
  http_req_failed:   ['rate<0.005'],        // < 0.5% de erros
  http_req_duration: ['p(95)<300'],
  http_req_duration: ['p(99)<800'],
};

// ── Wrappers ──────────────────────────────────────────────────────────────
export function post(url, body) {
  return http.post(url, JSON.stringify(body), { headers: HEADERS });
}

export function get(url) {
  return http.get(url, { headers: HEADERS });
}

export function put(url, body = {}) {
  return http.put(url, JSON.stringify(body), { headers: HEADERS });
}

export function del(url, body = {}) {
  return http.del(url, JSON.stringify(body), { headers: HEADERS });
}

// ── Checkers ──────────────────────────────────────────────────────────────
export function checkOk(res, name) {
  return check(res, {
    [`${name} → status 2xx`]: (r) => r.status >= 200 && r.status < 300,
    [`${name} → isSuccess=true`]: (r) => {
      try { return JSON.parse(r.body).isSuccess === true; } catch { return false; }
    },
  });
}

export function checkFail(res, name, code = 400) {
  return check(res, {
    [`${name} → status ${code}`]: (r) => r.status === code,
    [`${name} → isSuccess=false`]: (r) => {
      try { return JSON.parse(r.body).isSuccess === false; } catch { return false; }
    },
  });
}

export function extractId(res) {
  try { return JSON.parse(res.body).data; } catch { return null; }
}

// ── Dados de teste únicos ─────────────────────────────────────────────────
export function randomEmail() {
  return `user-${Date.now()}-${Math.random().toString(36).slice(2)}@test.com`;
}

export function randomSku() {
  return `SKU-${Date.now()}-${Math.random().toString(36).slice(2, 8).toUpperCase()}`;
}

export function customerPayload(email) {
  return {
    name: 'Cliente Teste K6',
    email: email || randomEmail(),
    phone: '11999999999',
    street: 'Rua K6, 100',
    city: 'São Paulo',
    state: 'SP',
    zipCode: '01310-100',
    country: 'Brasil',
  };
}

export function productPayload(sku) {
  return {
    name: 'Produto K6',
    description: 'Produto gerado pelo k6',
    sku: sku || randomSku(),
    price: 199.99,
    currency: 'BRL',
    initialStock: 9999,
    category: 'Testes',
  };
}

export function orderPayload(customerId, productId) {
  return {
    customerId,
    items: [{
      productId,
      productName: 'Produto K6',
      sku: 'SKU-K6-TEST',
      quantity: 1,
      unitPrice: 199.99,
      currency: 'BRL',
    }],
  };
}
