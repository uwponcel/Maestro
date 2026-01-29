namespace Maestro.Models
{
    public class UploadResponse
    {
        public bool Success { get; set; }
        public string PrUrl { get; set; }
        public string Error { get; set; }
        public string SongId { get; set; }
    }
}
