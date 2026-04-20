using Dapper;
using SentinelVault.Application.Interfaces;
using SentinelVault.Domain.Entities;
using SentinelVault.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelVault.Infrastructure.Repositories
{
    public class DocumentReposiotry(DbConnectionFactory connectionFactory) : IDocumentRepository
    {
        public async Task<Guid> SaveMetadataAsync(Document document)
        {
            using var connection = connectionFactory.CreateConnection();
            const string sql = @"
            INSERT INTO Documents (Id, FileName, FilePath, UploadedAt, Status, UserId)
            VALUES (@Id, @FileName, @FilePath, @UploadedAt, @Status, @UserId)";
            await connection.ExecuteAsync(sql, document);
            return document.Id;
        }
    }
}
