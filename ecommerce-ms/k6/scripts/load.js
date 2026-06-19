/**
 * LOAD TEST
 * Objetivo: simular carga normal de produção
 * Perfil: rampa até 50 VUs, sustentado por 5 min, descida suave
 */
import { sleep, group } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';
import {
  CUSTOMER_URL, PRODUCT_URL, ORDER_URL,
  post, get, put,
  checkOk, checkFail, extractId,
  customerPayload, productPayload, orderPayload,
  randomEmail, randomSku,
} from './helpers.js';

// ── Métricas customizadas ─────────────────────────────────────────────────
const orderCreated    = new Counter('orders_created_total');
const customerCreated = new Counter('customers_created_total');
const cacheHitRate    = new Rate('cache_hit_rate');
const orderDuration   = new Trend('order_flow_duration', true);

export const options = {
  stages: [
    { duration: '1m',  target: 10  },  // rampa lenta
    { duration: '2m',  target: 50  },  // sobe para carga normal
    { duration: '5m',  target: 50  },  // sustenta
    { duration: '1m',  target: 0   },  // descida
  ],
  thresholds: {
    http_req_failed:        ['rate<0.01'],
    http_req_duration:      ['p(95)<500', 'p(99)<1500'],
    orders_created_total:   ['count>10'],
    order_flow_duration:    ['p(95)<2000'],
  },
};

// Setup: cria produto fixo para ser reutilizado nos pedidos
export function setup() {
  const pRes = post(`${PRODUCT_URL}/api/products`, productPayload(`LOAD-${randomSku()}`));
  return { productId: extractId(pRes) };
}

export default function ({ productId }) {
  const start = Date.now();

  group('Customer Flow', () => {
    const cRes = post(`${CUSTOMER_URL}/api/customers`, customerPayload(randomEmail()));
    checkOk(cRes, 'criar customer');
    const customerId = extractId(cRes);
    customerCreated.add(1);

    if (customerId) {
      // Busca 1 — vai ao banco
      const r1 = get(`${CUSTOMER_URL}/api/customers/${customerId}`);
      checkOk(r1, 'GET customer 1ª vez');

      // Busca 2 — deve vir do cache (mesmo tempo ou menor)
      const t1 = Date.now();
      const r2 = get(`${CUSTOMER_URL}/api/customers/${customerId}`);
      const t2 = Date.now();
      checkOk(r2, 'GET customer 2ª vez (cache)');
      cacheHitRate.add(t2 - t1 < 50); // < 50ms considera cache hit
    }

    sleep(0.5);
  });

  group('Product Browse', () => {
    checkOk(get(`${PRODUCT_URL}/api/products`), 'listar produtos');
    if (productId) {
      checkOk(get(`${PRODUCT_URL}/api/products/${productId}`), 'buscar produto');
    }
    sleep(0.3);
  });

  group('Order Flow', () => {
    if (productId) {
      const cRes = post(`${CUSTOMER_URL}/api/customers`, customerPayload(randomEmail()));
      const customerId = extractId(cRes);

      if (customerId) {
        const oRes = post(`${ORDER_URL}/api/orders`, orderPayload(customerId, productId));
        checkOk(oRes, 'criar pedido');
        const orderId = extractId(oRes);
        orderCreated.add(1);

        if (orderId) {
          checkOk(get(`${ORDER_URL}/api/orders/${orderId}`), 'buscar pedido');

          // 70% confirma, 30% cancela
          if (Math.random() < 0.7) {
            checkOk(put(`${ORDER_URL}/api/orders/${orderId}/confirm`), 'confirmar pedido');
          } else {
            checkOk(put(`${ORDER_URL}/api/orders/${orderId}/cancel`, { reason: 'Teste k6' }), 'cancelar pedido');
          }
        }
      }
    }
    sleep(1);
  });

  orderDuration.add(Date.now() - start);
  sleep(Math.random() * 2 + 1); // think time realista
}
