import { Octokit } from 'octokit';
import { Env, SongData, CommitResult } from '../types';
import { generateSongId, toBase64, fromBase64 } from '../utils';

interface ManifestSong {
  id: string;
  name: string;
  artist: string;
  transcriber: string;
  instrument: string;
  durationMs: number;
  createdAt: string;
}

interface Manifest {
  version: number;
  lastUpdated: string;
  songs: ManifestSong[];
}

const TARGET_BRANCH = 'bhud-static/Aex.Maestro';
const NAMESPACE = 'community-pending';

export async function commitSongToBranch(
  env: Env,
  song: SongData,
  transcriber: string,
  existingSongId?: string,
  durationMs?: number
): Promise<CommitResult> {
  const octokit = new Octokit({ auth: env.GITHUB_BOT_TOKEN });

  const isUpdate = !!existingSongId;
  const songId = existingSongId || generateSongId();

  // Read manifest first — fail early before committing song file
  const manifestData = await readManifest(octokit, env);

  // Commit the song file (create or overwrite)
  await commitSongFile(octokit, env, songId, song, transcriber);

  // Update the manifest (add new entry or replace existing)
  const resolvedDuration = durationMs || computeDuration(song.notes);
  await updateManifest(
    octokit,
    env,
    manifestData,
    songId,
    song,
    transcriber,
    resolvedDuration,
    isUpdate
  );

  return { songId };
}

function serializeSong(song: SongData, transcriber: string): string {
  return JSON.stringify(
    {
      name: song.name,
      artist: song.artist || 'Unknown',
      transcriber,
      instrument: song.instrument,
      notes: song.notes,
      skipOctaveReset: song.skipOctaveReset || false,
    },
    null,
    2
  );
}

async function commitSongFile(
  octokit: Octokit,
  env: Env,
  songId: string,
  song: SongData,
  transcriber: string
): Promise<void> {
  const filePath = `${NAMESPACE}/songs/${songId}.json`;
  const content = serializeSong(song, transcriber);
  const encodedContent = toBase64(content);

  let sha: string | undefined;
  try {
    const { data } = await octokit.rest.repos.getContent({
      owner: env.GITHUB_TARGET_OWNER,
      repo: env.GITHUB_TARGET_REPO,
      path: filePath,
      ref: TARGET_BRANCH,
    });
    if ('sha' in data) {
      sha = data.sha;
    }
  } catch {
    // File doesn't exist yet, that's fine
  }

  const commitMessage = `${sha ? 'Update' : 'Add'} song: ${song.name} by ${song.artist || 'Unknown'}`;

  await octokit.rest.repos.createOrUpdateFileContents({
    owner: env.GITHUB_TARGET_OWNER,
    repo: env.GITHUB_TARGET_REPO,
    path: filePath,
    message: commitMessage,
    content: encodedContent,
    branch: TARGET_BRANCH,
    ...(sha ? { sha } : {}),
  });
}

interface ManifestFileData {
  content: string;
  sha: string;
}

async function readManifest(octokit: Octokit, env: Env): Promise<ManifestFileData> {
  const manifestPath = `${NAMESPACE}/manifest.json`;

  const { data: fileData } = await octokit.rest.repos.getContent({
    owner: env.GITHUB_TARGET_OWNER,
    repo: env.GITHUB_TARGET_REPO,
    path: manifestPath,
    ref: TARGET_BRANCH,
  });

  if (!('content' in fileData)) {
    throw new Error('Manifest file not found');
  }

  return { content: fileData.content, sha: fileData.sha };
}

async function updateManifest(
  octokit: Octokit,
  env: Env,
  manifestData: ManifestFileData,
  songId: string,
  song: SongData,
  transcriber: string,
  durationMs: number,
  isUpdate: boolean = false
): Promise<void> {
  const manifestPath = `${NAMESPACE}/manifest.json`;

  const currentManifest: Manifest = JSON.parse(fromBase64(manifestData.content));

  const songEntry: ManifestSong = {
    id: songId,
    name: song.name || 'Unknown',
    artist: song.artist || 'Unknown',
    transcriber,
    instrument: song.instrument || 'Piano',
    durationMs,
    createdAt: new Date().toISOString(),
  };

  if (isUpdate) {
    const index = currentManifest.songs.findIndex((s) => s.id === songId);
    if (index >= 0) {
      songEntry.createdAt = currentManifest.songs[index].createdAt;
      currentManifest.songs[index] = songEntry;
    } else {
      currentManifest.songs.push(songEntry);
    }
  } else {
    currentManifest.songs.push(songEntry);
  }

  currentManifest.lastUpdated = new Date().toISOString();

  const updatedContent = JSON.stringify(currentManifest, null, 2);
  const encodedContent = toBase64(updatedContent);

  await octokit.rest.repos.createOrUpdateFileContents({
    owner: env.GITHUB_TARGET_OWNER,
    repo: env.GITHUB_TARGET_REPO,
    path: manifestPath,
    message: `Update manifest: add ${song.name}`,
    content: encodedContent,
    sha: manifestData.sha,
    branch: TARGET_BRANCH,
  });
}

function computeDuration(notes?: string[]): number {
  if (!notes || notes.length === 0) return 0;
  return notes.reduce((total, note) => {
    const parts = note.split(':');
    return total + (parseInt(parts[1], 10) || 0);
  }, 0);
}
