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
            public string Username { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterDto request)
        {
            try
            {
                // 1. Crear el usuario
                var newUser = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = request.Password // (En prod usaríamos hash real, para dev está bien)
                };

                // 2. Guardar en DB
                _context.Users.Add(newUser);
                _context.SaveChanges();

                return Ok(new { message = "User created!", token = "fake-jwt-token-for-dev" });
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
                    createdAt = user.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal Server Error: {ex.Message}", details = ex.InnerException?.Message });
            }
        }
    }
}
