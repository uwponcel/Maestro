import { Env } from './types';
import { corsHeaders, jsonResponse } from './utils';
import { handleUpload } from './handlers/upload';

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    // Handle CORS preflight
    if (request.method === 'OPTIONS') {
      return new Response(null, { headers: corsHeaders() });
    }

    const url = new URL(request.url);

    // Route: POST /api/upload-song
    if (request.method === 'POST' && url.pathname.endsWith('/upload-song')) {
      try {
        return await handleUpload(request, env);
      } catch (error) {
        console.error('Upload error:', error);
        return jsonResponse({ success: false, error: 'Internal server error' }, 500);
      }
    }

    // Health check: GET /
    if (request.method === 'GET' && (url.pathname === '/' || url.pathname === '/api')) {
      return jsonResponse({ status: 'ok', service: 'maestro-api' });
    }

    return jsonResponse({ error: 'Not found' }, 404);
  },
};

export type { Env };
