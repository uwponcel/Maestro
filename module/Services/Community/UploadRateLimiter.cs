using System;
using Blish_HUD;
using LiteDB;

namespace Maestro.Services.Community
{
    public class UploadRateLimiter
    {
        private static readonly Logger Logger = Logger.GetLogger<UploadRateLimiter>();

        private const int MAX_UPLOADS_PER_DAY = 3;
        private const string RATE_LIMIT_COLLECTION = "upload_rate_limit";

        private readonly ILiteCollection<UploadRecord> _collection;
        private int _todayCount;
        private string _todayKey;

        public UploadRateLimiter(LiteDatabase database)
        {
            _collection = database.GetCollection<UploadRecord>(RATE_LIMIT_COLLECTION);
            LoadTodayCount();
        }

        public int GetRemainingUploads()
        {
            EnsureTodayKey();
            return Math.Max(0, MAX_UPLOADS_PER_DAY - _todayCount);
        }

        public bool CanUpload()
        {
            return GetRemainingUploads() > 0;
        }

        public void RecordUpload()
        {
            EnsureTodayKey();
            _todayCount++;

            var record = _collection.FindById(_todayKey);

            if (record == null)
            {
                record = new UploadRecord
                {
                    Id = _todayKey,
                    Date = DateTime.UtcNow.Date,
                    Count = _todayCount
                };
                _collection.Insert(record);
            }
            else
            {
                record.Count = _todayCount;
                _collection.Update(record);
            }

            Logger.Info($"Recorded upload. Uploads today: {_todayCount}/{MAX_UPLOADS_PER_DAY}");
        }

        public void CleanupOldRecords()
        {
            var cutoff = DateTime.UtcNow.Date.AddDays(-7);
            var deleted = _collection.DeleteMany(r => r.Date < cutoff);
            if (deleted > 0)
            {
                Logger.Debug($"Cleaned up {deleted} old upload rate limit records");
            }
        }

        private void LoadTodayCount()
        {
            _todayKey = GetTodayKey();
            var record = _collection.FindById(_todayKey);
            _todayCount = record?.Count ?? 0;
        }

        private void EnsureTodayKey()
        {
            var currentKey = GetTodayKey();
            if (_todayKey != currentKey)
            {
                _todayKey = currentKey;
                _todayCount = 0;
            }
        }

        private string GetTodayKey()
        {
            return DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        }

        private class UploadRecord
        {
            public string Id { get; set; }
            public DateTime Date { get; set; }
            public int Count { get; set; }
        }
    }
}
