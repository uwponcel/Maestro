export interface Env {
  RATE_LIMIT: KVNamespace;
  GITHUB_BOT_TOKEN: string;
  GITHUB_TARGET_OWNER: string;
  GITHUB_TARGET_REPO: string;
}

export interface SongData {
  name?: string;
  artist?: string;
  transcriber?: string;
  instrument?: string;
  notes?: string[];
  skipOctaveReset?: boolean;
}

export interface UploadRequest {
  song?: SongData;
  transcriber?: string;
  clientId?: string;
  existingSongId?: string;
  durationMs?: number;
}

export interface UploadResponse {
  success: boolean;
  error?: string;
  songId?: string;
}

export interface CommitResult {
  songId: string;
}
