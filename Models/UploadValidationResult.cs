namespace Maestro.Models
{
    public class UploadValidationResult
    {
        public bool IsValid => NameValid && TranscriberValid && InstrumentValid &&
                               NotesValid && !IsDuplicate && !RateLimitExceeded;

        public bool NameValid { get; set; }
        public string NameError { get; set; }

        public bool TranscriberValid { get; set; }
        public string TranscriberError { get; set; }

        public bool InstrumentValid { get; set; }
        public string InstrumentError { get; set; }

        public bool NotesValid { get; set; }
        public string NotesError { get; set; }

        public bool IsDuplicate { get; set; }
        public string DuplicateError { get; set; }

        public bool RateLimitExceeded { get; set; }
        public string RateLimitError { get; set; }

        public static UploadValidationResult CreateValid()
        {
            return new UploadValidationResult
            {
                NameValid = true,
                TranscriberValid = true,
                InstrumentValid = true,
                NotesValid = true,
                IsDuplicate = false,
                RateLimitExceeded = false
            };
        }
    }
}
