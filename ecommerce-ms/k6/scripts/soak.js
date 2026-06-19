/**
 * SOAK TEST
 * Objetivo: detectar memory leaks e degradação ao longo do tempo
 * Perfil: 30 VUs por 30 minutos contínuos
 */
import { sleep, group } from 'k6';
import { Trend, Counter } from 'k6/metrics';
import {
  CUSTOMER_URL, PRODUCT_URL, ORDER_URL,
  post, get, put, checkOk, extractId,
  customerPayload, productPayload, orderPayload,
  randomEmail, randomSku,
} from './helpers.js';

const p95Over5min  = new Trend('p95_rolling');
const totalOrders  = new Counter('soak_orders_total');

export const options = {
  stages: [
    { duration: '2m',  target: 30 },  // aquecimento
    { duration: '26m', target: 30 },  // soak
    { duration: '2m',  target: 0  },  // resfriamento
  ],
  thresholds: {
    http_req_failed:   ['rate<0.01'],
    http_req_duration: ['p(95)<800'],  // mais restrito — detecta degradação
    soak_orders_total: ['count>100'],
  },
};

export function setup() {
  const pRes = post(`${PRODUCT_URL}/api/products`, productPayload(`SOAK-${randomSku()}`));
  return { productId: extractId(pRes) };
}

export default function ({ productId }) {
  const t0 = Date.now();

  group('Soak Cycle', () => {
    // Health check periódico
    checkOk(get(`${CUSTOMER_URL}/health`), 'health customer');
    checkOk(get(`${PRODUCT_URL}/health`), 'health product');
    checkOk(get(`${ORDER_URL}/health`), 'health order');

    sleep(0.5);

    // Listagem (detecta lentidão por acúmulo de dados)
    checkOk(get(`${PRODUCT_URL}/api/products`), 'listar produtos');
    checkOk(get(`${CUSTOMER_URL}/api/customers`), 'listar clientes');

    sleep(0.5);

    // Fluxo de pedido completo
    if (productId && Math.random() < 0.5) {
      const cRes = post(`${CUSTOMER_URL}/api/customers`, customerPayload(randomEmail()));
      const customerId = extractId(cRes);
      if (customerId) {
        const oRes = post(`${ORDER_URL}/api/orders`, orderPayload(customerId, productId));
        checkOk(oRes, 'criar pedido soak');
        const orderId = extractId(oRes);
        if (orderId) {
          totalOrders.add(1);
          checkOk(put(`${ORDER_URL}/api/orders/${orderId}/confirm`), 'confirmar pedido soak');
        }
      }
    }
  });

  p95Over5min.add(Date.now() - t0);
  sleep(2 + Math.random() * 2);
}
