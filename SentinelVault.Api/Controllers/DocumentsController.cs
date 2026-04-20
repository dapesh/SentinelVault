using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SentinelVault.Application.Interfaces;
using SentinelVault.Domain.Entities;

namespace SentinelVault.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController(IDocumentRepository repository) : ControllerBase
    {
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            // 1. Define local storage path
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "LocalVault");
            if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

            var filePath = Path.Combine(uploadsPath, file.FileName);

            // 2. Save physical file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 3. Create Metadata record
            var document = new Document
            {
                FileName = file.FileName,
                FilePath = filePath,
                UserId = Guid.NewGuid()
            };

            await repository.SaveMetadataAsync(document);

            return Ok(new { document.Id, message = "File secured in vault. AI processing started." });
        }
    }
}
