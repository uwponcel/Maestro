using System;
using System.Collections.Generic;
using Maestro.Models;

namespace Maestro.Services
{
    public enum RepeatMode
    {
        Off,
        All,
        One
    }

    /// <summary>
    /// Non-destructive playback queue with a cursor. The queue is the source of
    /// truth for the whole session; <see cref="Current"/> points at the playing
    /// song and <see cref="MoveNext"/> advances it according to repeat/shuffle.
    /// Songs are NOT removed as they play.
    /// </summary>
    public class PlaylistService
    {
        public event EventHandler QueueChanged;      // structural change (add/remove/move/clear)
        public event EventHandler CurrentChanged;    // the current song changed
        public event EventHandler RepeatModeChanged;
        public event EventHandler ShuffleChanged;

        private readonly List<Song> _queue = new List<Song>();
        private readonly Random _random = new Random();
        private List<Song> _shuffle;   // active order when shuffling; null otherwise

        private RepeatMode _repeat = RepeatMode.Off;
        private bool _shuffleOn;
        private int _index = -1;       // cursor into the active order

        public IReadOnlyList<Song> Queue => _queue;
        public int Count => _queue.Count;
        public bool HasItems => _queue.Count > 0;

        private List<Song> Active => _shuffleOn ? _shuffle : _queue;

        /// <summary>The currently-selected song, or null.</summary>
        public Song Current => (_index >= 0 && _index < Active.Count) ? Active[_index] : null;

        /// <summary>Visible-queue index of the current song, or -1. Used for the drawer highlight.</summary>
        public int CurrentIndex
        {
            get
            {
                var cur = Current;
                return cur != null ? _queue.IndexOf(cur) : -1;
            }
        }

        public RepeatMode Repeat
        {
            get => _repeat;
            set
            {
                if (_repeat == value) return;
                _repeat = value;
                RepeatModeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool Shuffle
        {
            get => _shuffleOn;
            set
            {
                if (_shuffleOn == value) return;

                var cur = Current;
                _shuffleOn = value;

                if (_shuffleOn)
                {
                    BuildShuffle(cur);
                }
                else
                {
                    _shuffle = null;
                    _index = cur != null ? _queue.IndexOf(cur) : -1;
                }

                ShuffleChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Add(Song song)
        {
            if (song == null) return;

            _queue.Add(song);
            if (_shuffleOn)
                _shuffle.Add(song);

            QueueChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Remove(Song song)
        {
            var visibleIndex = _queue.IndexOf(song);
            if (visibleIndex >= 0)
                RemoveAt(visibleIndex);
        }

        public void RemoveAt(int visibleIndex)
        {
            if (visibleIndex < 0 || visibleIndex >= _queue.Count) return;

            var song = _queue[visibleIndex];
            var wasCurrent = Current == song;
            var activeIndex = Active.IndexOf(song);

            _queue.RemoveAt(visibleIndex);
            if (_shuffleOn)
                _shuffle.Remove(song);

            // Keep the cursor on the same logical position. Removing the current
            // song decrements the cursor so the next MoveNext lands on the song
            // that shifted into the gap (no skip); the already-playing audio of a
            // removed current song finishes naturally before that advance.
            if (activeIndex >= 0 && activeIndex <= _index)
                _index--;

            QueueChanged?.Invoke(this, EventArgs.Empty);
            if (wasCurrent)
                CurrentChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Move(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _queue.Count) return;
            if (toIndex < 0 || toIndex >= _queue.Count) return;
            if (fromIndex == toIndex) return;

            var cur = Current;

            var item = _queue[fromIndex];
            _queue.RemoveAt(fromIndex);
            _queue.Insert(toIndex, item);

            // A visible reorder only moves the cursor when not shuffling
            // (while shuffling, playback follows the separate shuffle order).
            if (!_shuffleOn && cur != null)
                _index = _queue.IndexOf(cur);

            QueueChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            if (_queue.Count == 0) return;

            _queue.Clear();
            _shuffle?.Clear();
            _index = -1;

            QueueChanged?.Invoke(this, EventArgs.Empty);
            CurrentChanged?.Invoke(this, EventArgs.Empty);
        }

        public int IndexOf(Song song) => _queue.IndexOf(song);

        /// <summary>Begin playback from the top of the active order.</summary>
        public void StartPlayback()
        {
            if (_shuffleOn)
                BuildShuffle(null);

            _index = _queue.Count > 0 ? 0 : -1;
            CurrentChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Advance the cursor per repeat/shuffle. Returns false when playback
        /// should stop (end of queue under Repeat.Off, or empty queue).
        /// </summary>
        public bool MoveNext()
        {
            if (_queue.Count == 0)
            {
                _index = -1;
                return false;
            }

            if (_repeat == RepeatMode.One && Current != null)
                return true;

            var next = _index + 1;

            if (next >= Active.Count)
            {
                if (_repeat == RepeatMode.All)
                {
                    if (_shuffleOn)
                        BuildShuffle(null);   // fresh order each loop
                    next = 0;
                }
                else
                {
                    _index = Active.Count;    // park past the end
                    CurrentChanged?.Invoke(this, EventArgs.Empty);
                    return false;
                }
            }

            _index = next;
            CurrentChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        private void BuildShuffle(Song first)
        {
            _shuffle = new List<Song>(_queue);

            for (var i = _shuffle.Count - 1; i > 0; i--)
            {
                var j = _random.Next(i + 1);
                var tmp = _shuffle[i];
                _shuffle[i] = _shuffle[j];
                _shuffle[j] = tmp;
            }

            if (first != null && _shuffle.Remove(first))
            {
                _shuffle.Insert(0, first);
                _index = 0;
            }
        }
    }
}
