using SentinelVault.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelVault.Application.Interfaces
{
    public interface IDocumentRepository
    {
        Task<Guid> SaveMetadataAsync(Document document);
    }
}
