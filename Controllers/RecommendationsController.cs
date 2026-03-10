using FataleCore.Services.Intelligence;
using Microsoft.AspNetCore.Mvc;

namespace FataleCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationsController : ControllerBase
    {
        private readonly IIntelligenceService _intelligenceService;

        public RecommendationsController(IIntelligenceService intelligenceService)
        {
            _intelligenceService = intelligenceService;
        }

        [HttpGet("next")]
        public async Task<IActionResult> GetNext([FromHeader(Name = "UserId")] int userId, [FromQuery] string lastVideoId, [FromQuery] string lastTrackType = "youtube", [FromQuery] int count = 3)
        {
            if (userId <= 0) return Unauthorized("UserId is required.");

            try
            {
                var recommendations = await _intelligenceService.GetRecommendationsAsync(userId, lastVideoId, lastTrackType, count);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to generate recommendations", error = ex.Message });
            }
        }
    }
}
