using System;
using System.Collections.Generic;
using Maestro.Models;

namespace Maestro.Services
{
    public class PlaylistService
    {
        public event EventHandler QueueChanged;

        private readonly List<Song> _queue = new List<Song>();

        public IReadOnlyList<Song> Queue => _queue;
        public int Count => _queue.Count;
        public bool HasItems => _queue.Count > 0;

        public void Add(Song song)
        {
            if (song == null) return;

            _queue.Add(song);
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Remove(Song song)
        {
            if (_queue.Remove(song))
            {
                QueueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void RemoveAt(int index)
        {
            if (index >= 0 && index < _queue.Count)
            {
                _queue.RemoveAt(index);
                QueueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Move(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _queue.Count) return;
            if (toIndex < 0 || toIndex >= _queue.Count) return;
            if (fromIndex == toIndex) return;

            var item = _queue[fromIndex];
            _queue.RemoveAt(fromIndex);
            _queue.Insert(toIndex, item);
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            if (_queue.Count > 0)
            {
                _queue.Clear();
                QueueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public Song Dequeue()
        {
            if (_queue.Count == 0) return null;

            var song = _queue[0];
            _queue.RemoveAt(0);
            QueueChanged?.Invoke(this, EventArgs.Empty);
            return song;
        }

        public Song Peek()
        {
            return _queue.Count > 0 ? _queue[0] : null;
        }

        public int IndexOf(Song song)
        {
            return _queue.IndexOf(song);
        }
    }
}
