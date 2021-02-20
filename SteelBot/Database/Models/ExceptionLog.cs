using System;

namespace SteelBot.Database.Models
{
    public class ExceptionLog
    {
        public long RowId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string SourceMethod { get; set; }
        public string FullDetail { get; set; }

        /// <summary>
        /// Empty constructor.
        /// Used by EF - do not remove.
        /// </summary>
        public ExceptionLog() { }

        public ExceptionLog(Exception ex, string sourceMethod)
        {
            Timestamp = DateTime.UtcNow;
            Message = ex.Message;
            StackTrace = ex.StackTrace;
            SourceMethod = sourceMethod;
            FullDetail = ex.ToString();
        }
    }
}