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

        public UploadRateLimiter(LiteDatabase database)
        {
            _collection = database.GetCollection<UploadRecord>(RATE_LIMIT_COLLECTION);
        }

        public int GetRemainingUploads()
        {
            var todayKey = GetTodayKey();
            var record = _collection.FindById(todayKey);

            if (record == null)
            {
                return MAX_UPLOADS_PER_DAY;
            }

            return Math.Max(0, MAX_UPLOADS_PER_DAY - record.Count);
        }

        public bool CanUpload()
        {
            return GetRemainingUploads() > 0;
        }

        public void RecordUpload()
        {
            var todayKey = GetTodayKey();
            var record = _collection.FindById(todayKey);

            if (record == null)
            {
                record = new UploadRecord
                {
                    Id = todayKey,
                    Date = DateTime.UtcNow.Date,
                    Count = 1
                };
                _collection.Insert(record);
            }
            else
            {
                record.Count++;
                _collection.Update(record);
            }

            Logger.Info($"Recorded upload. Uploads today: {record.Count}/{MAX_UPLOADS_PER_DAY}");
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
