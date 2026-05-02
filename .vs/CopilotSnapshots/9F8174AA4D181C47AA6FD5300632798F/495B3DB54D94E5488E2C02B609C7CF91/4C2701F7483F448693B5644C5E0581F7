using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelVault.Domain.Entities
{
    public class Document
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending";
    }
}
