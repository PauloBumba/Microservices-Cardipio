/**
 * STRESS TEST
 * Objetivo: encontrar o ponto de ruptura do sistema
 * Perfil: sobe agressivamente até 200 VUs
 */
import { sleep, group } from 'k6';
import { Rate } from 'k6/metrics';
import {
  CUSTOMER_URL, PRODUCT_URL, ORDER_URL,
  post, get, put, checkOk, extractId,
  customerPayload, productPayload, orderPayload,
  randomEmail, randomSku,
} from './helpers.js';

const errorRate = new Rate('errors');

export const options = {
  stages: [
    { duration: '2m',  target: 50  },
    { duration: '3m',  target: 100 },
    { duration: '3m',  target: 150 },
    { duration: '3m',  target: 200 },
    { duration: '2m',  target: 0   },
  ],
  thresholds: {
    http_req_failed:   ['rate<0.05'],   // aceita até 5% de erro em stress
    http_req_duration: ['p(95)<2000'],  // 95% < 2s
    errors:            ['rate<0.05'],
  },
};

export function setup() {
  const pRes = post(`${PRODUCT_URL}/api/products`, productPayload(`STRESS-${randomSku()}`));
  return { productId: extractId(pRes) };
}

export default function ({ productId }) {
  // Alterna entre leitura pesada e escrita
  const scenario = Math.random();

  if (scenario < 0.5) {
    // 50% — leitura intensiva (mais comum em produção)
    group('Read Heavy', () => {
      const r1 = get(`${PRODUCT_URL}/api/products`);
      errorRate.add(r1.status !== 200);
      checkOk(r1, 'listar produtos');

      if (productId) {
        const r2 = get(`${PRODUCT_URL}/api/products/${productId}`);
        errorRate.add(r2.status !== 200);
        checkOk(r2, 'buscar produto');
      }
      sleep(0.1);
    });
  } else if (scenario < 0.8) {
    // 30% — criação de clientes
    group('Write Customer', () => {
      const r = post(`${CUSTOMER_URL}/api/customers`, customerPayload(randomEmail()));
      errorRate.add(r.status !== 201);
      checkOk(r, 'criar customer');
      sleep(0.2);
    });
  } else {
    // 20% — fluxo completo de pedido
    group('Full Order Flow', () => {
      const cRes = post(`${CUSTOMER_URL}/api/customers`, customerPayload(randomEmail()));
      const customerId = extractId(cRes);
      if (customerId && productId) {
        const oRes = post(`${ORDER_URL}/api/orders`, orderPayload(customerId, productId));
        errorRate.add(oRes.status !== 201);
        checkOk(oRes, 'criar pedido stress');
      }
      sleep(0.5);
    });
  }
}
