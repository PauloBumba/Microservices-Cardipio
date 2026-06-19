/**
 * SMOKE TEST
 * Objetivo: sanidade básica — 1 VU, 1 min
 * Valida que todos os endpoints respondem corretamente antes de qualquer load.
 */
import { sleep } from 'k6';
import {
  CUSTOMER_URL, PRODUCT_URL, ORDER_URL,
  post, get, put, del,
  checkOk, checkFail, extractId,
  customerPayload, productPayload, orderPayload,
  randomEmail, randomSku,
} from './helpers.js';

export const options = {
  vus: 1,
  duration: '1m',
  thresholds: {
    http_req_failed:   ['rate<0.01'],
    http_req_duration: ['p(95)<1000'],
  },
};

export default function () {
  // ── Customer ────────────────────────────────────────────────────────────
  const cRes = post(`${CUSTOMER_URL}/api/customers`, customerPayload(randomEmail()));
  checkOk(cRes, 'POST /customers');
  const customerId = extractId(cRes);

  if (customerId) {
    checkOk(get(`${CUSTOMER_URL}/api/customers/${customerId}`), 'GET /customers/:id');
    checkOk(get(`${CUSTOMER_URL}/api/customers`), 'GET /customers');
    checkOk(put(`${CUSTOMER_URL}/api/customers/${customerId}`, {
      name: 'Atualizado', phone: '11988887777',
      street: 'Rua Nova', city: 'SP', state: 'SP', zipCode: '01000-000', country: 'Brasil',
    }), 'PUT /customers/:id');
  }

  // Validação FluentValidation
  checkFail(post(`${CUSTOMER_URL}/api/customers`, { name: '', email: 'invalido', phone: '' }), 'POST /customers validation');

  sleep(1);

  // ── Product ─────────────────────────────────────────────────────────────
  const pRes = post(`${PRODUCT_URL}/api/products`, productPayload(randomSku()));
  checkOk(pRes, 'POST /products');
  const productId = extractId(pRes);

  if (productId) {
    checkOk(get(`${PRODUCT_URL}/api/products/${productId}`), 'GET /products/:id');
    checkOk(get(`${PRODUCT_URL}/api/products`), 'GET /products');
    checkOk(post(`${PRODUCT_URL}/api/products/${productId}/stock`, { quantity: 10 }), 'POST /products/stock');
  }

  sleep(1);

  // ── Order ───────────────────────────────────────────────────────────────
  if (customerId && productId) {
    const oRes = post(`${ORDER_URL}/api/orders`, orderPayload(customerId, productId));
    checkOk(oRes, 'POST /orders');
    const orderId = extractId(oRes);

    if (orderId) {
      checkOk(get(`${ORDER_URL}/api/orders/${orderId}`), 'GET /orders/:id');
      checkOk(put(`${ORDER_URL}/api/orders/${orderId}/confirm`), 'PUT /orders/confirm');

      // Confirmar novamente deve falhar
      checkFail(put(`${ORDER_URL}/api/orders/${orderId}/confirm`), 'PUT /orders/confirm (duplicado)');
    }

    // Pedido sem itens
    checkFail(post(`${ORDER_URL}/api/orders`, { customerId, items: [] }), 'POST /orders (sem itens)');
  }

  // ── Health checks ────────────────────────────────────────────────────────
  checkOk(get(`${CUSTOMER_URL}/health`), 'GET /health customer');
  checkOk(get(`${PRODUCT_URL}/health`), 'GET /health product');
  checkOk(get(`${ORDER_URL}/health`), 'GET /health order');

  sleep(2);
}
