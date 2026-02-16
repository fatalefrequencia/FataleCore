using Microsoft.AspNetCore.Http;

namespace FataleCore.Dtos
{
    public class UpdateProfileDto
    {
        public string Username { get; set; } = string.Empty;
        public string Biography { get; set; } = string.Empty;
        public IFormFile? ProfilePicture { get; set; }
    }
}
