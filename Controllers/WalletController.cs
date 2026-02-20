using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WalletController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Wallet/transactions
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromHeader(Name = "UserId")] int userId, [FromQuery] string? type, [FromQuery] int limit = 20, [FromQuery] int offset = 0)
        {
            var query = _context.Transactions
                .Where(t => t.UserId == userId);

            if (!string.IsNullOrEmpty(type) && type != "ALL")
            {
                // Multi-type filtering if needed, or simple exact match
                if (type == "EARNINGS")
                {
                    query = query.Where(t => t.Type == "EARNING_SALE" || t.Type == "TIP_RECEIVED");
                }
                else
                {
                    query = query.Where(t => t.Type == type);
                }
            }

            var total = await query.CountAsync();

            var transactions = await query
                .OrderByDescending(t => t.Timestamp)
                .Skip(offset)
                .Take(limit)
                .Select(t => new
                {
                    t.Id,
                    t.Type,
                    t.Amount,
                    t.Description,
                    t.Timestamp,
                    t.RelatedUserId,
                    t.TrackId
                })
                .ToListAsync();

            return Ok(new { total, data = transactions });
        }

        // GET: api/Wallet/earnings/summary
        [HttpGet("earnings/summary")]
        public async Task<IActionResult> GetEarningsSummary([FromHeader(Name = "UserId")] int userId)
        {
            // Calculate total earnings
            var earnings = await _context.Transactions
                .Where(t => t.UserId == userId && (t.Type == "EARNING_SALE" || t.Type == "TIP_RECEIVED"))
                .SumAsync(t => t.Amount);

            var purchases = await _context.Transactions
                .Where(t => t.UserId == userId && (t.Type == "EARNING_SALE"))
                .SumAsync(t => t.Amount);

            var tips = await _context.Transactions
                .Where(t => t.UserId == userId && (t.Type == "TIP_RECEIVED"))
                .SumAsync(t => t.Amount);

            // Last 30 days
            var last30Days = await _context.Transactions
                 .Where(t => t.UserId == userId && 
                             (t.Type == "EARNING_SALE" || t.Type == "TIP_RECEIVED") &&
                             t.Timestamp >= DateTime.UtcNow.AddDays(-30))
                 .SumAsync(t => t.Amount);

            return Ok(new 
            { 
                totalEarnings = earnings,
                breakdown = new 
                {
                    sales = purchases,
                    tips = tips
                },
                last30Days
            });
        }

        // POST: api/Wallet/transfer
        [HttpPost("transfer")]
        public async Task<IActionResult> TransferCredits([FromBody] TransferRequest request, [FromHeader(Name = "UserId")] int userId)
        {
            if (request.Amount <= 0) return BadRequest("Transfer amount must be positive");
            if (userId == request.ToUserId) return BadRequest("Cannot transfer to self");

            var sender = await _context.Users.FindAsync(userId);
            if (sender == null) return NotFound("Sender not found");

            if (sender.CreditsBalance < request.Amount)
                return BadRequest("Insufficient credits");

            var receiver = await _context.Users.FindAsync(request.ToUserId);
            if (receiver == null) return NotFound("Receiver not found");

            // Execute Transfer
            sender.CreditsBalance -= request.Amount;
            receiver.CreditsBalance += request.Amount;

            // Log for Sender
            _context.Transactions.Add(new Transaction
            {
                UserId = sender.Id,
                Type = "TRANSFER_SENT",
                Amount = -request.Amount,
                Description = $"Transfer to {receiver.Username}", // Using username if available, or just ID log
                RelatedUserId = receiver.Id,
                Timestamp = DateTime.UtcNow
            });

            // Log for Receiver
            _context.Transactions.Add(new Transaction
            {
                UserId = receiver.Id,
                Type = "TRANSFER_RECEIVED",
                Amount = request.Amount,
                Description = $"Transfer from {sender.Username}",
                RelatedUserId = sender.Id,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { success = true, newBalance = sender.CreditsBalance, message = $"Transferred {request.Amount} credits to {receiver.Username}" });
        }

        // POST: api/Wallet/withdraw/request
        [HttpPost("withdraw/request")]
        public async Task<IActionResult> RequestWithdrawal([FromBody] WithdrawalRequest request, [FromHeader(Name = "UserId")] int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            if (request.Amount <= 0) return BadRequest("Amount must be positive");
            if (user.CreditsBalance < request.Amount) return BadRequest("Insufficient credits");

            // For now, just a placeholder. In future, create a WithdrawalRecord entity.
            // We will deduct credits immediately to "lock" them.
            
            user.CreditsBalance -= request.Amount;

            _context.Transactions.Add(new Transaction
            {
                UserId = user.Id,
                Type = "WITHDRAWAL",
                Amount = -request.Amount,
                Description = $"Withdrawal request ({request.Method})",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { success = true, newBalance = user.CreditsBalance, message = "Withdrawal request submitted. Credits deducted pending processing." });
        }

        public class TransferRequest
        {
            public int ToUserId { get; set; }
            public int Amount { get; set; }
        }

        public class WithdrawalRequest
        {
            public int Amount { get; set; }
            public string Method { get; set; } = "Stripe";
        }
    }
}
