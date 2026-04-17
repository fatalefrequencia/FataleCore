using FataleCore.Data;
using FataleCore.DTOs;
using FataleCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public StudioController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyGallery([FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");
            
            var contents = await _context.StudioContents
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
                
            return Ok(contents);
        }

        [HttpGet("user/{targetUserId}")]
        public async Task<IActionResult> GetUserGallery(int targetUserId)
        {
            var contents = await _context.StudioContents
                .Where(c => c.UserId == targetUserId && c.IsPosted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
                
            return Ok(contents);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadContent([FromForm] StudioContentUploadDto dto, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");
            if (dto.File == null || dto.File.Length == 0) return BadRequest("File is required");

            try
            {
                var folder = dto.Type.ToUpper() == "VIDEO" ? "videos" : "photos";
                var appBase = _env.IsProduction() ? "/data" : Directory.GetCurrentDirectory();
                var uploadsPath = Path.Combine(appBase, "uploads", "studio", folder);
                if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.File.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.File.CopyToAsync(stream);
                }

                var content = new StudioContent
                {
                    UserId = userId,
                    Title = dto.Title ?? dto.File.FileName,
                    Description = dto.Description,
                    Url = $"/uploads/studio/{folder}/{fileName}",
                    Type = dto.Type.ToUpper(),
                    IsPosted = dto.IsPosted
                };

                if (dto.ThumbnailFile != null && dto.ThumbnailFile.Length > 0)
                {
                    var thumbFolder = "thumbnails";
                    var thumbUploadsPath = Path.Combine(appBase, "uploads", "studio", thumbFolder);
                    if (!Directory.Exists(thumbUploadsPath)) Directory.CreateDirectory(thumbUploadsPath);

                    var thumbFileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.ThumbnailFile.FileName)}";
                    var thumbFilePath = Path.Combine(thumbUploadsPath, thumbFileName);

                    using (var stream = new FileStream(thumbFilePath, FileMode.Create))
                    {
                        await dto.ThumbnailFile.CopyToAsync(stream);
                    }
                    content.ThumbnailUrl = $"/uploads/studio/{thumbFolder}/{thumbFileName}";
                }

                _context.StudioContents.Add(content);
                await _context.SaveChangesAsync();

                return Ok(content);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("toggle-pin/{id}")]
        public async Task<IActionResult> TogglePin(int id, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var content = await _context.StudioContents.FindAsync(id);
            if (content == null) return NotFound();
            if (content.UserId != userId) return Forbid();

            content.IsPinned = !content.IsPinned;
            await _context.SaveChangesAsync();

            return Ok(new { isPinned = content.IsPinned });
        }

        [HttpPost("toggle-post/{id}")]
        public async Task<IActionResult> TogglePost(int id, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var content = await _context.StudioContents.FindAsync(id);
            if (content == null) return NotFound();
            if (content.UserId != userId) return Forbid();

            content.IsPosted = !content.IsPosted;
            await _context.SaveChangesAsync();

            return Ok(new { isPosted = content.IsPosted });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContent(int id, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var content = await _context.StudioContents.FindAsync(id);
            if (content == null) return NotFound();
            if (content.UserId != userId) return Forbid();

            _context.StudioContents.Remove(content);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Content deleted successfully" });
        }

        [HttpPost("update-thumbnail/{id}")]
        public async Task<IActionResult> UpdateThumbnail(int id, [FromForm] IFormFile file, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");
            if (file == null || file.Length == 0) return BadRequest("File is required");

            var content = await _context.StudioContents.FindAsync(id);
            if (content == null) return NotFound();
            if (content.UserId != userId) return Forbid();

            try
            {
                var thumbFolder = "thumbnails";
                var appBase = _env.IsProduction() ? "/data" : Directory.GetCurrentDirectory();
                var uploadsPath = Path.Combine(appBase, "uploads", "studio", thumbFolder);
                if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                content.ThumbnailUrl = $"/uploads/studio/{thumbFolder}/{fileName}";
                await _context.SaveChangesAsync();

                return Ok(new { thumbnailUrl = content.ThumbnailUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
