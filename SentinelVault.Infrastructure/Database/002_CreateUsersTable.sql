-- PostgreSQL Script to create Users table

CREATE TABLE IF NOT EXISTS Users
(
    Id UUID NOT NULL PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastLogin TIMESTAMP NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE
);

-- Create indexes for common queries
CREATE INDEX IF NOT EXISTS IX_Users_Email ON Users (Email);
CREATE INDEX IF NOT EXISTS IX_Users_IsActive ON Users (IsActive);
