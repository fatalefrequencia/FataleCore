using Microsoft.AspNetCore.Http;

namespace FataleCore.Dtos
{
    public class UpdateProfileDto
    {
        public string Username { get; set; }
        public string Biography { get; set; }
        public IFormFile ProfilePicture { get; set; }
    }
}
