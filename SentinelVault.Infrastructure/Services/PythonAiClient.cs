using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SentinelVault.Application.Interfaces;
using SentinelVault.Domain.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SentinelVault.Infrastructure.Services
{
    public class PythonAiClient : IPythonAiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PythonAiClient> _logger;
        private readonly IServiceProvider _serviceProvider;

        public PythonAiClient(HttpClient httpClient, ILogger<PythonAiClient> logger, IServiceProvider serviceProvider)
        {
            _httpClient = httpClient;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<string> GetAiResponseAsync(string query)
        {
            try
            {
                _logger.LogInformation("Sending query to SentinelVault RAG API: {Query}", query);
                var requestPayload = new QueryRequest { Query = query };
                var response = await _httpClient.PostAsJsonAsync("query", requestPayload);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<QueryResponse>();
                    if (result != null)
                    {
                        _logger.LogInformation("AI service returned response. Confidence: {Confidence}", result.Confidence);
                        return result.Answer;
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("AI service returned error: {StatusCode}. Details: {Error}", response.StatusCode, errorContent);
                return "Failed to retrieve a response from the AI service.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to RAG service for query.");
                return "Sorry, I'm having trouble connecting to my brain right now.";
            }
        }

        public async Task<bool> UploadDocumentAsync(Guid documentId, string fileName, Stream fileStream)
        {
            try
            {
                // Set status to Processing in DB
                await UpdateStatusHelperAsync(documentId, DocumentStatus.Processing);

                using var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                content.Add(fileContent, "file", fileName); // Field name must be "file"

                _logger.LogInformation("Uploading {FileName} to SentinelVault RAG API...", fileName);
                var response = await _httpClient.PostAsync("ingest", content);

                if (response.StatusCode == HttpStatusCode.Accepted ||      // 202 Success
                    response.StatusCode == HttpStatusCode.MultiStatus ||   // 207 Partial Success
                    response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<IngestResponse>();
                    if (result != null && (result.Status == "success" || result.Status == "partial_success"))
                    {
                        if (Guid.TryParse(result.DocumentId, out var externalId))
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                            await repository.UpdateExternalDocumentIdAsync(documentId, externalId);
                            _logger.LogInformation("Successfully mapped local document {DocumentId} to external ID {ExternalId}", documentId, externalId);
                        }

                        await UpdateStatusHelperAsync(documentId, DocumentStatus.Completed);
                        _logger.LogInformation("Successfully ingested {FileName}. Status: {Status}", fileName, result.Status);
                        return true;
                    }
                }

                // If not successful
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ingestion failed for {FileName}. Code: {Code}. Details: {Details}", fileName, response.StatusCode, errorBody);
                await UpdateStatusHelperAsync(documentId, DocumentStatus.Failed);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during document upload.");
                await UpdateStatusHelperAsync(documentId, DocumentStatus.Failed);
                return false;
            }
        }

        private async Task UpdateStatusHelperAsync(Guid documentId, DocumentStatus status)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                await repository.UpdateStatusAsync(documentId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update document status to {Status} in DB helper.", status);
            }
        }

        #region Internal Models
        private class IngestResponse
        {
            [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
            [JsonPropertyName("document_id")] public string? DocumentId { get; set; }
            [JsonPropertyName("total_chunks")] public int TotalChunks { get; set; }
        }

        private class QueryRequest
        {
            [JsonPropertyName("query")] public string Query { get; set; } = string.Empty;
            [JsonPropertyName("document_id")] public string? DocumentId { get; set; }
        }

        private class QueryResponse
        {
            [JsonPropertyName("answer")] public string Answer { get; set; } = string.Empty;
            [JsonPropertyName("confidence")] public float Confidence { get; set; }
        }
        #endregion
    }
}
