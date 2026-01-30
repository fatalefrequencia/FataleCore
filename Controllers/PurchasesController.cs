using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchasesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PurchasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Purchases
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TrackPurchase>>> GetPurchases([FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized(new { message = "Invalid User ID" });

            return await _context.TrackPurchases
                .Where(tp => tp.UserId == userId)
                .Include(tp => tp.Track)
                .ThenInclude(t => t!.Album)
                .ThenInclude(a => a!.Artist)
                .ToListAsync();
        }
    }
}
