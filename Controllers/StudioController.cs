using FataleCore.Data;
using FataleCore.Dtos;
using FataleCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StudioController(ApplicationDbContext context)
        {
            _context = context;
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
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "studio", folder);
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
    }
}
