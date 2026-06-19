/**
 * SPIKE TEST
 * Objetivo: simular pico repentino de tráfego (ex: flash sale)
 * Perfil: salta de 5 para 250 VUs instantaneamente, depois cai
 */
import { sleep } from 'k6';
import { Rate } from 'k6/metrics';
import {
  CUSTOMER_URL, PRODUCT_URL, ORDER_URL,
  post, get, put, checkOk, extractId,
  customerPayload, productPayload, orderPayload,
  randomEmail, randomSku,
} from './helpers.js';

const errorRate = new Rate('spike_errors');

export const options = {
  stages: [
    { duration: '10s', target: 5   },  // linha de base
    { duration: '30s', target: 250 },  // SPIKE — pico repentino
    { duration: '1m',  target: 250 },  // sustenta o pico
    { duration: '30s', target: 5   },  // recuperação
    { duration: '30s', target: 0   },  // descida
  ],
  thresholds: {
    http_req_failed:   ['rate<0.10'],   // aceita até 10% durante spike
    http_req_duration: ['p(95)<3000'],
    spike_errors:      ['rate<0.10'],
  },
};

export function setup() {
  const pRes = post(`${PRODUCT_URL}/api/products`, productPayload(`SPIKE-${randomSku()}`));
  return { productId: extractId(pRes) };
}

export default function ({ productId }) {
  // Durante spike: foca em listagem (cenário mais comum em flash sale)
  const r = get(`${PRODUCT_URL}/api/products`);
  errorRate.add(r.status !== 200);
  checkOk(r, 'listar produtos (spike)');

  if (productId) {
    checkOk(get(`${PRODUCT_URL}/api/products/${productId}`), 'buscar produto (spike)');
  }

  // Simula usuários tentando criar pedido durante o spike
  if (Math.random() < 0.3) {
    const cRes = post(`${CUSTOMER_URL}/api/customers`, customerPayload(randomEmail()));
    const customerId = extractId(cRes);
    if (customerId && productId) {
      const oRes = post(`${ORDER_URL}/api/orders`, orderPayload(customerId, productId));
      errorRate.add(oRes.status !== 201);
    }
  }

  sleep(0.1);
}
