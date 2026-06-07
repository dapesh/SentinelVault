using Dapper;
using SentinelVault.Application.Interfaces;
using SentinelVault.Domain.Entities;
using SentinelVault.Domain.Enums;
using SentinelVault.Infrastructure.Persistence;

namespace SentinelVault.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for document persistence using Dapper.
    /// </summary>
    public class DocumentReposiotry : IDocumentRepository
    {
        private readonly DbConnectionFactory _connectionFactory;
        private static bool _schemaChecked = false;
        private static readonly object _schemaLock = new();

        public DocumentReposiotry(DbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            EnsureExternalDocumentIdColumnExists();
        }

        private void EnsureExternalDocumentIdColumnExists()
        {
            if (_schemaChecked) return;
            lock (_schemaLock)
            {
                if (_schemaChecked) return;
                try
                {
                    using var connection = _connectionFactory.CreateConnection();
                    connection.Execute("ALTER TABLE Documents ADD COLUMN IF NOT EXISTS ExternalDocumentId UUID;");
                    _schemaChecked = true;
                }
                catch
                {
                    // Ignore schema checking errors to prevent application startup blockage
                }
            }
        }

        /// <summary>
        /// Saves document metadata to the database.
        /// </summary>
        public async Task<Guid> SaveMetadataAsync(Document document)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
            INSERT INTO Documents (Id, FileName, FilePath, UploadedAt, Status, UserId, ExternalDocumentId)
            VALUES (@Id, @FileName, @FilePath, @UploadedAt, @Status, @UserId, @ExternalDocumentId)";

            await connection.ExecuteAsync(sql, new
            {
                document.Id,
                document.FileName,
                document.FilePath,
                document.UploadedAt,
                Status = (int)document.Status,
                document.UserId,
                document.ExternalDocumentId
            });

            return document.Id;
        }

        /// <summary>
        /// Retrieves a document by its ID.
        /// </summary>
        public async Task<Document?> GetByIdAsync(Guid id)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
            SELECT Id, FileName, FilePath, UploadedAt, Status, UserId, ExternalDocumentId
            FROM Documents
            WHERE Id = @Id";

            var result = await connection.QueryFirstOrDefaultAsync<Document>(sql, new { Id = id });
            return result;
        }

        /// <summary>
        /// Retrieves all documents for a specific user.
        /// </summary>
        public async Task<IEnumerable<Document>> GetByUserIdAsync(Guid userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
            SELECT Id, FileName, FilePath, UploadedAt, Status, UserId, ExternalDocumentId
            FROM Documents
            WHERE UserId = @UserId
            ORDER BY UploadedAt DESC";

            var results = await connection.QueryAsync<Document>(sql, new { UserId = userId });
            return results.ToList();
        }

        /// <summary>
        /// Updates the status of a document.
        /// </summary>
        public async Task UpdateStatusAsync(Guid id, DocumentStatus status)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
            UPDATE Documents
            SET Status = @Status
            WHERE Id = @Id";

            await connection.ExecuteAsync(sql, new { Status = (int)status, Id = id });
        }

        /// <summary>
        /// Updates the external document ID returned from the RAG service.
        /// </summary>
        public async Task UpdateExternalDocumentIdAsync(Guid id, Guid externalDocumentId)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
            UPDATE Documents
            SET ExternalDocumentId = @ExternalDocumentId
            WHERE Id = @Id";

            await connection.ExecuteAsync(sql, new { ExternalDocumentId = externalDocumentId, Id = id });
        }

        /// <summary>
        /// Maps dynamic query result to Document domain entity.
        /// Handles enum conversion from int Status column.
        /// </summary>
        private static Document MapToDomain(dynamic result)
        {
            return new Document
            {
                Id = (Guid)result.Id,
                FileName = (string)result.FileName,
                FilePath = (string)result.FilePath,
                UploadedAt = (DateTime)result.UploadedAt,
                Status = (DocumentStatus)(int)result.Status,
                UserId = (Guid)result.UserId,
                CreatedDate = (DateTime)result.CreatedDate,
                ExternalDocumentId = result.ExternalDocumentId != null ? (Guid)result.ExternalDocumentId : null
            };
        }
    }
}

