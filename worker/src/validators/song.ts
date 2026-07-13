import { UploadRequest } from '../types';

const VALID_INSTRUMENTS = ['Piano', 'Harp', 'Lute', 'Bass'];
const MIN_NAME_LENGTH = 3;
const MIN_TRANSCRIBER_LENGTH = 2;
const MIN_NOTE_COUNT = 10;

export function validateSong(request: UploadRequest): string | null {
  if (!request.song) {
    return 'Song data is required';
  }

  const song = request.song;

  // Validate name
  if (!song.name?.trim()) {
    return 'Song name is required';
  }
  if (song.name.trim().length < MIN_NAME_LENGTH) {
    return `Song name must be at least ${MIN_NAME_LENGTH} characters`;
  }

  // Validate transcriber
  const transcriber = request.transcriber || song.transcriber;
  if (!transcriber?.trim()) {
    return 'Transcriber name is required';
  }
  if (transcriber.trim().length < MIN_TRANSCRIBER_LENGTH) {
    return `Transcriber must be at least ${MIN_TRANSCRIBER_LENGTH} characters`;
  }

  // Validate instrument
  if (!song.instrument) {
    return 'Instrument is required';
  }
  const isValidInstrument = VALID_INSTRUMENTS.some(
    (i) => i.toLowerCase() === song.instrument!.toLowerCase()
  );
  if (!isValidInstrument) {
    return `Invalid instrument: ${song.instrument}`;
  }

  // Validate notes
  if (!song.notes || song.notes.length === 0) {
    return 'Song must have notes';
  }

  const noteCount = song.notes.filter((n) => !n.startsWith('R:')).length;
  if (noteCount < MIN_NOTE_COUNT) {
    return `Song must have at least ${MIN_NOTE_COUNT} notes (has ${noteCount})`;
  }

  return null;
}
