using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EconomyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EconomyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Economy/balance
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance([FromHeader(Name = "UserId")] int userId)
        {
            // Dev Mode Fallback
            var user = await _context.Users.FindAsync(userId);
            if (user == null) user = await _context.Users.FirstOrDefaultAsync();

            if (user == null) return NotFound("User not found (No users in DB)");

            return Ok(new { balance = user.CreditsBalance });
        }

        // POST: api/Economy/purchase/{trackId}
        [HttpPost("purchase/{trackId}")]
        public async Task<IActionResult> PurchaseTrack(int trackId, [FromHeader(Name = "UserId")] int userId)
        {
            // Dev Mode Fallback
            var user = await _context.Users.FindAsync(userId);
            if (user == null) user = await _context.Users.FirstOrDefaultAsync();

            if (user == null) return NotFound("User not found");

            var track = await _context.Tracks
                .Include(t => t.Album)
                .FirstOrDefaultAsync(t => t.Id == trackId);

            if (track == null) return NotFound("Track not found");

            // Check if already purchased
            var alreadyPurchased = await _context.TrackPurchases
                .AnyAsync(tp => tp.UserId == user.Id && tp.TrackId == trackId);
            
            if (alreadyPurchased) return BadRequest(new { message = "Track already purchased" });

            int cost = track.Price; 

            if (user.CreditsBalance < cost)
            {
                return BadRequest(new { message = "Insufficient credits" });
            }

            // Deduct credits
            user.CreditsBalance -= cost;

            // Record purchase
            var purchase = new TrackPurchase
            {
                UserId = user.Id,
                TrackId = trackId,
                Cost = cost,
                PurchaseDate = DateTime.UtcNow
            };

            _context.TrackPurchases.Add(purchase);

            // 1. Transaction Log: Buyer (Spending)
            _context.Transactions.Add(new Transaction
            {
                UserId = user.Id,
                Type = "PURCHASE",
                Amount = -cost,
                Description = $"Purchased track: {track.Title}",
                TrackId = trackId,
                Timestamp = DateTime.UtcNow
            });

            // Credit the Artist (and associated User)
            if (track.Album?.ArtistId != null)
            {
                var artist = await _context.Artists.FindAsync(track.Album.ArtistId);
                if (artist != null)
                {
                    artist.CreditsBalance += cost;
                    
                    // IF Artist is linked to a User, credit the User too
                    if (artist.UserId.HasValue)
                    {
                        var artistUser = await _context.Users.FindAsync(artist.UserId.Value);
                        if (artistUser != null)
                        {
                            artistUser.CreditsBalance += cost;

                            // 2. Transaction Log: Artist User (Earning)
                            _context.Transactions.Add(new Transaction
                            {
                                UserId = artistUser.Id,
                                Type = "EARNING_SALE",
                                Amount = cost,
                                Description = $"Sale of track: {track.Title}",
                                TrackId = trackId,
                                RelatedUserId = user.Id, // Buyer
                                Timestamp = DateTime.UtcNow
                            });
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, newBalance = user.CreditsBalance, message = "Track purchased successfully" });
        }

        // POST: api/Economy/tip/{artistId}?amount=50
        [HttpPost("tip/{artistId}")]
        public async Task<IActionResult> TipArtist(int artistId, [FromQuery] int amount, [FromHeader(Name = "UserId")] int userId)
        {
            // Dev Mode Fallback
            var user = await _context.Users.FindAsync(userId);
            if (user == null) user = await _context.Users.FirstOrDefaultAsync();

            if (user == null) return NotFound("User not found");

            int tipAmount = amount > 0 ? amount : 50; // Default to 50 if not specified

            if (user.CreditsBalance < tipAmount)
                 return BadRequest(new { message = "Insufficient credits for tip" });

            user.CreditsBalance -= tipAmount;
            
            // 1. Transaction Log: Sender (Tip Sent)
            _context.Transactions.Add(new Transaction
            {
                UserId = user.Id,
                Type = "TIP_SENT",
                Amount = -tipAmount,
                Description = $"Tip to artist ID {artistId}",
                RelatedUserId = artistId, // Linking to artist ID for now (or resolution below)
                Timestamp = DateTime.UtcNow
            });
            
            // Credit the Artist
            var artist = await _context.Artists.FindAsync(artistId);
            if (artist != null)
            {
                artist.CreditsBalance += tipAmount;

                // IF Artist is linked to a User, credit the User too
                if (artist.UserId.HasValue)
                {
                    var artistUser = await _context.Users.FindAsync(artist.UserId.Value);
                    if (artistUser != null)
                    {
                        artistUser.CreditsBalance += tipAmount;

                        // 2. Transaction Log: Artist User (Tip Received)
                        _context.Transactions.Add(new Transaction
                        {
                            UserId = artistUser.Id,
                            Type = "TIP_RECEIVED",
                            Amount = tipAmount,
                            Description = $"Tip from {user.Username}",
                            RelatedUserId = user.Id,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            
            return Ok(new { success = true, newBalance = user.CreditsBalance, message = $"Tipped {tipAmount} credits!" });
        }

        // POST: api/Economy/add
        [HttpPost("add")]
        public async Task<IActionResult> AddCredits([FromBody] AddCreditsDto request, [FromHeader(Name = "UserId")] int userId)
        {
            // Dev Mode Fallback: If userId is 0 or invalid, try to get the first user
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                 // Fallback to first user for dev convenience
                 user = await _context.Users.FirstOrDefaultAsync();
            }

            if (user == null) return NotFound("User not found and no users available in DB");

            if (request.Amount <= 0) return BadRequest(new { message = "Amount must be positive" });

            user.CreditsBalance += request.Amount;
            
            _context.Transactions.Add(new Transaction
            {
                UserId = user.Id,
                Type = "DEPOSIT",
                Amount = request.Amount,
                Description = "Manual credit adjustment",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { success = true, newBalance = user.CreditsBalance, message = $"Added {request.Amount} credits" });
        }

        public class AddCreditsDto
        {
            public int Amount { get; set; }
        }
    }
}
