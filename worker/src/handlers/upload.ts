import { Env, UploadRequest } from '../types';
import { jsonResponse } from '../utils';
import { validateSong } from '../validators/song';
import { checkRateLimit, recordUpload } from '../services/rate-limiter';
import { commitSongToBranch } from '../services/github';

export async function handleUpload(request: Request, env: Env): Promise<Response> {
  const clientIp = getClientIp(request);
  const isDebug = request.headers.get('X-Debug-Key') === 'maestro-debug';

  // Parse request body
  const uploadRequest = await parseRequestBody(request);
  if (!uploadRequest) {
    return jsonResponse({ success: false, error: 'Invalid JSON in request body' }, 400);
  }

  const clientId = uploadRequest.clientId;

  // Check rate limit (skip in debug mode)
  if (!isDebug) {
    const canUpload = await checkRateLimit(env, clientIp, clientId);
    if (!canUpload) {
      return jsonResponse(
        { success: false, error: 'Daily upload limit exceeded (3 per day). Try again tomorrow.' },
        429
      );
    }
  }

  // Validate song
  const validationError = validateSong(uploadRequest);
  if (validationError) {
    return jsonResponse({ success: false, error: validationError }, 400);
  }

  const song = uploadRequest.song!;
  const transcriber = uploadRequest.transcriber || song.transcriber!;

  // Commit song to the community-pending/ namespace on bhud-static/Aex.Maestro
  try {
    const { songId } = await commitSongToBranch(
      env,
      song,
      transcriber,
      uploadRequest.existingSongId,
      uploadRequest.durationMs
    );

    // Record upload for rate limiting (skip in debug mode)
    if (!isDebug) {
      await recordUpload(env, clientIp, clientId);
    }

    return jsonResponse({ success: true, songId });
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : String(error);
    console.error('GitHub API error:', errorMessage, error);
    return jsonResponse({ success: false, error: `GitHub error: ${errorMessage}` }, 500);
  }
}

function getClientIp(request: Request): string {
  return request.headers.get('CF-Connecting-IP') || 'unknown';
}

async function parseRequestBody(request: Request): Promise<UploadRequest | null> {
  try {
    return await request.json();
  } catch {
    return null;
  }
}
