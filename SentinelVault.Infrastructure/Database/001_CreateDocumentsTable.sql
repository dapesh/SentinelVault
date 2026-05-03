-- PostgreSQL Script to create Documents table

CREATE TABLE IF NOT EXISTS Documents
(
    Id UUID NOT NULL PRIMARY KEY,
    FileName VARCHAR(255) NOT NULL,
    FilePath VARCHAR(512) NOT NULL,
    UserId UUID NOT NULL,
    CreatedDate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UploadedAt TIMESTAMP NOT NULL,
    Status INT NOT NULL DEFAULT 0,
    CreatedAtUtc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for common queries
CREATE INDEX IF NOT EXISTS IX_Documents_UserId ON Documents (UserId);
CREATE INDEX IF NOT EXISTS IX_Documents_Status ON Documents (Status);
CREATE INDEX IF NOT EXISTS IX_Documents_CreatedDate ON Documents (CreatedDate);
