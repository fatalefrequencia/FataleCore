using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile([FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) 
            {
                 // Dev fallback: try first user
                 var firstUser = await _context.Users.FirstOrDefaultAsync();
                 if (firstUser != null) return Ok(firstUser);
                 return Unauthorized("Invalid User ID");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            return Ok(user);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] FataleCore.Dtos.UpdateProfileDto dto, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            // Update text fields if provided
            if (!string.IsNullOrEmpty(dto.Username)) user.Username = dto.Username;
            if (dto.Biography != null) user.Biography = dto.Biography; // Allow empty string to clear bio via DTO if needed using explicit empty string, but null means no change usually. Let's assume frontend sends empty string to clear.

            // Handle Profile Picture
            if (dto.ProfilePicture != null && dto.ProfilePicture.Length > 0)
            {
                // Ensure directory exists
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "avatars");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.ProfilePicture.FileName)}";
                var filePath = Path.Combine(uploadPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ProfilePicture.CopyToAsync(stream);
                }

                // Update URL (Frontend should serve static files from /uploads or via a controller)
                // Assuming we serve static files or have an endpoint. Ideally we store relative path.
                user.ProfilePictureUrl = $"/uploads/avatars/{fileName}";
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile updated successfully", user });
        }
    }
}
