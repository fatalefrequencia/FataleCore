using Microsoft.AspNetCore.Mvc;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public FileController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty.");

            // Create uploads folder if it doesn't exist
            var appBase = _environment.IsProduction() ? "/data" : Directory.GetCurrentDirectory();
            var uploadsText = Path.Combine(appBase, "uploads");
            if (!Directory.Exists(uploadsText))
            {
                Directory.CreateDirectory(uploadsText);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsText, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path for client use
            // Assuming static files are served from content root, but usually they are served from wwwroot.
            // Let's stick effectively to serving from specific StaticFile middleware or similar.
            // For simplicity in a basic backend, serving directly from a known folder via StaticFiles is best.

            // Changing strategy: Save to wwwroot/uploads if possible, or just ContentRoot/uploads and map it.
            // Let's use ContentRoot/uploads and we will configure StaticFiles to serve it.
            
            var relativePath = $"/uploads/{fileName}";
            return Ok(new { path = relativePath });
        }
    }
}
