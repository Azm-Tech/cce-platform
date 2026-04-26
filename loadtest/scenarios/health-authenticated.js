// k6 — authenticated load test against Internal API.
// Acquires a service-account token from Keycloak's cce-admin-cms client once at setup,
// then reuses it across all VUs.

import http from 'k6/http';
import { check } from 'k6';

const API_BASE_URL = __ENV.API_BASE_URL || 'http://api-internal:5002';
const KEYCLOAK_URL = __ENV.KEYCLOAK_URL || 'http://keycloak:8080';
const CLIENT_ID = __ENV.OIDC_CLIENT_ID || 'cce-admin-cms';
const CLIENT_SECRET = __ENV.OIDC_CLIENT_SECRET || 'dev-internal-secret-change-me';

export const options = {
  scenarios: {
    authenticated_health: {
      executor: 'constant-vus',
      vus: 50,
      duration: '60s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<200'],
  },
};

export function setup() {
  const tokenResp = http.post(
    `${KEYCLOAK_URL}/realms/cce-internal/protocol/openid-connect/token`,
    {
      grant_type: 'client_credentials',
      client_id: CLIENT_ID,
      client_secret: CLIENT_SECRET,
    },
    { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } },
  );
  if (tokenResp.status !== 200) {
    throw new Error(`token acquisition failed: ${tokenResp.status} ${tokenResp.body}`);
  }
  const token = JSON.parse(tokenResp.body).access_token;
  return { token };
}

export default function (data) {
  // Hit /auth/echo (which only requires authentication, not SuperAdmin policy).
  const res = http.get(`${API_BASE_URL}/auth/echo`, {
    headers: { Authorization: `Bearer ${data.token}` },
  });
  check(res, {
    'status 200 or 403': (r) => r.status === 200 || r.status === 403,
  });
}
