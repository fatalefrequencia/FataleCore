using Microsoft.AspNetCore.Mvc;
using FataleCore.Data; // Asegúrate de que el namespace coincida con tu proyecto
using FataleCore.Models;
namespace FataleCore.Controllers { 
    [Route("api/[controller]")] 
    [ApiController] 
    public class AuthController : ControllerBase { 
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Clase simple para recibir los datos del Frontend
        public class RegisterDto
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterDto request)
        {
            try
            {
                // 0. Validation
                if (_context.Users.Any(u => u.Username == request.Username))
                    return BadRequest(new { error = "Protocol Error: Username is already allocated in the core." });
                
                if (_context.Users.Any(u => u.Email == request.Email))
                    return BadRequest(new { error = "Protocol Error: Email transmission already registered." });

                // 1. Crear el usuario
                var newUser = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = request.Password, // (En prod usaríamos hash real, para dev está bien)
                    Biography = "",
                    ProfilePictureUrl = "",
                    BannerUrl = "",
                    WallpaperVideoUrl = ""
                };

                // 2. Create automatic Artist Profile (Hidden by default)
                var artist = new Artist
                {
                    Name = newUser.Username,
                    Bio = "New Artist Profile", // Hidden from map
                    ImageUrl = "", // No default image
                    User = newUser // EF handles the FK assignment after user gets Id
                };

                // 3. Save both in a single transaction
                _context.Users.Add(newUser);
                _context.Artists.Add(artist);
                _context.SaveChanges();

                return Ok(new 
                { 
                    message = "Identity Synchronized!", 
                    token = "fake-jwt-token-for-dev",
                    userId = newUser.Id,
                    username = newUser.Username,
                    email = newUser.Email,
                    createdAt = newUser.CreatedAt,
                    // Personalization Defaults
                    themeColor = newUser.ThemeColor,
                    textColor = newUser.TextColor,
                    backgroundColor = newUser.BackgroundColor,
                    isGlass = newUser.IsGlass,
                    bannerUrl = newUser.BannerUrl,
                    creditsBalance = newUser.CreditsBalance,
                    communityId = newUser.CommunityId
                });
            }
            catch (Exception ex)
            {
                // Si falla, dime EXACTAMENTE por qué
                return BadRequest(new { error = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        public class LoginDto
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto request)
        {
            try
            {
                var user = _context.Users.FirstOrDefault(u => u.Username == request.Username);
                
                if (user == null || user.PasswordHash != request.Password) // En prod usar hash
                {
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                return Ok(new 
                { 
                    token = "fake-jwt-token-for-dev", 
                    userId = user.Id,
                    username = user.Username,
                    email = user.Email,
                    createdAt = user.CreatedAt,
                    // Personalization
                    themeColor = user.ThemeColor,
                    textColor = user.TextColor,
                    backgroundColor = user.BackgroundColor,
                    isGlass = user.IsGlass,
                    bannerUrl = user.BannerUrl,
                    profilePictureUrl = user.ProfilePictureUrl,
                    biography = user.Biography,
                    creditsBalance = user.CreditsBalance,
                    communityId = user.CommunityId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal Server Error: {ex.Message}", details = ex.InnerException?.Message });
            }
        }
    }
}
