import { Env } from '../types';
import { getDateKey } from '../utils';

const MAX_UPLOADS_PER_DAY = 3;
const TTL_SECONDS = 86400; // 24 hours

export async function checkRateLimit(
  env: Env,
  clientIp: string,
  clientId?: string
): Promise<boolean> {
  const ipKey = getRateLimitKey('ip', clientIp);
  const ipCount = await getCurrentCount(env, ipKey);
  if (ipCount >= MAX_UPLOADS_PER_DAY) return false;

  if (clientId) {
    const clientKey = getRateLimitKey('client', clientId);
    const clientCount = await getCurrentCount(env, clientKey);
    if (clientCount >= MAX_UPLOADS_PER_DAY) return false;
  }

  return true;
}

export async function recordUpload(env: Env, clientIp: string, clientId?: string): Promise<void> {
  const ipKey = getRateLimitKey('ip', clientIp);
  const ipCount = await getCurrentCount(env, ipKey);
  await env.RATE_LIMIT.put(ipKey, String(ipCount + 1), {
    expirationTtl: TTL_SECONDS,
  });

  if (clientId) {
    const clientKey = getRateLimitKey('client', clientId);
    const clientCount = await getCurrentCount(env, clientKey);
    await env.RATE_LIMIT.put(clientKey, String(clientCount + 1), {
      expirationTtl: TTL_SECONDS,
    });
  }
}

export async function getRemainingUploads(
  env: Env,
  clientIp: string,
  clientId?: string
): Promise<number> {
  const ipKey = getRateLimitKey('ip', clientIp);
  const ipCount = await getCurrentCount(env, ipKey);
  let remaining = Math.max(0, MAX_UPLOADS_PER_DAY - ipCount);

  if (clientId) {
    const clientKey = getRateLimitKey('client', clientId);
    const clientCount = await getCurrentCount(env, clientKey);
    remaining = Math.min(remaining, Math.max(0, MAX_UPLOADS_PER_DAY - clientCount));
  }

  return remaining;
}

function getRateLimitKey(type: string, identifier: string): string {
  return `rate:${type}:${identifier}:${getDateKey()}`;
}

async function getCurrentCount(env: Env, key: string): Promise<number> {
  const value = await env.RATE_LIMIT.get(key);
  return parseInt(value || '0', 10);
}
