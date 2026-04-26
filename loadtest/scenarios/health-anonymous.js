// k6 — anonymous /health load test
// Run: k6 run loadtest/scenarios/health-anonymous.js
// Or via Docker:  docker compose --profile loadtest run --rm k6 run /scenarios/health-anonymous.js

import http from 'k6/http';
import { check } from 'k6';

const API_BASE_URL = __ENV.API_BASE_URL || 'http://api-external:5001';

export const options = {
  scenarios: {
    anonymous_health: {
      executor: 'constant-vus',
      vus: 100,
      duration: '60s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],          // <1% errors
    http_req_duration: ['p(95)<100'],        // p95 below 100ms
  },
};

export default function () {
  const res = http.get(`${API_BASE_URL}/health`, {
    headers: { 'Accept-Language': 'ar' },
  });
  check(res, {
    'status 200': (r) => r.status === 200,
    'has status:ok': (r) => typeof r.body === 'string' && r.body.includes('"status":"ok"'),
  });
}
