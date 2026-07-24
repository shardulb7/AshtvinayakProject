using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace AshtavinayakAPP.Controllers
{
    // MED-13: was class-level Admin-only, which blocked every user from viewing their own
    // transaction history. GET actions are now open to any authenticated user but scoped to
    // the caller's own records (Admins still see everything); mutations remain Admin-only.
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly AshtvinayakTravelContext _context;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(AshtvinayakTravelContext context, ILogger<TransactionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Transactions?page=1&pageSize=20
        [HttpGet]
        public async Task<ActionResult<object>> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                // MED-03: guard page and pageSize against invalid values
                page     = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                IQueryable<Transaction> query = _context.Transactions.Where(x => !x.IsDeleted)
                    .Include(t => t.Booking)
                    .Include(t => t.User);

                // MED-13: non-Admin callers only ever see their own transactions.
                if (!User.IsInRole("Admin"))
                {
                    var callerId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                    query = query.Where(t => t.UserId != null && t.UserId.ToString() == callerId);
                }

                var totalCount  = await query.CountAsync();
                var totalPages  = (int)Math.Ceiling((double)totalCount / pageSize);

                var transactions = await query
                    .OrderByDescending(t => t.TransactionId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        TransactionId   = t.TransactionId,
                        BookingCode     = t.Booking.BookingCode,
                        UserName        = t.User.UserName,
                        Amount          = t.Amount,
                        TransactionDate = t.TransactionDate,
                        PaymentStatus   = t.PaymentStatus,
                        PaymentMethod   = t.PaymentMethod
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Message      = "Transactions fetched successfully.",
                    TotalCount   = totalCount,
                    TotalPages   = totalPages,
                    CurrentPage  = page,
                    PageSize     = pageSize,
                    Data         = transactions
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "GetTransactions failed for page={Page} pageSize={PageSize}", page, pageSize);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        // GET: api/Transactions/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTransaction(int id)
        {
            try
            {
                var transactionQuery = _context.Transactions.Where(x => !x.IsDeleted)
                    .Include(t => t.Booking)
                    .Include(t => t.User)
                    .Where(t => t.TransactionId == id);

                // MED-13: non-Admin callers may only fetch their own transaction by id.
                if (!User.IsInRole("Admin"))
                {
                    var callerId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                    transactionQuery = transactionQuery.Where(t => t.UserId != null && t.UserId.ToString() == callerId);
                }

                var transaction = await transactionQuery
                    .Select(t => new
                    {
                        TransactionId = t.TransactionId,
                        BookingCode = t.Booking.BookingCode,
                        UserName = t.User.UserName,
                        Amount = t.Amount,
                        TransactionDate = t.TransactionDate,
                        PaymentStatus=t.PaymentStatus,
                        PaymentMethod = t.PaymentMethod


                    })
                    .FirstOrDefaultAsync();

                if (transaction == null)
                {
                    return NotFound("Transaction not found.");
                }

                return Ok(new
                {
                    Message = "Transaction fetched successfully.",
                    Data = transaction
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "GetTransaction failed for TransactionId={TransactionId}", id);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        // POST: api/Transactions
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<object>> PostTransaction([FromBody] Transaction transaction)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var bookingExists = await _context.Bookings.AnyAsync(b => b.BookingId == transaction.BookingId);
                if (!bookingExists)
                {
                    return BadRequest("Invalid BookingId.");
                }

                var userExists = await _context.Users.AnyAsync(u => u.UserId == transaction.UserId);
                if (!userExists)
                {
                    return BadRequest("Invalid UserId.");
                }

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Transaction created successfully.",
                    Data = transaction
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "PostTransaction failed for BookingId={BookingId}", transaction.BookingId);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        // PUT: api/Transactions/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTransaction(int id, [FromBody] Transaction transaction)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (id != transaction.TransactionId)
                {
                    return BadRequest("Transaction ID mismatch.");
                }

                var existingTransaction = await _context.Transactions.FindAsync(id);
                if (existingTransaction == null)
                {
                    return NotFound("Transaction not found.");
                }

                existingTransaction.BookingId        = transaction.BookingId;
                existingTransaction.UserId            = transaction.UserId;
                existingTransaction.Amount            = transaction.Amount;
                existingTransaction.TransactionDate   = transaction.TransactionDate;
                existingTransaction.PaymentMethod     = transaction.PaymentMethod;
                existingTransaction.PaymentStatus     = transaction.PaymentStatus; // HIGH-02: was missing



                _context.Entry(existingTransaction).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Transaction updated successfully.",
                    Data = existingTransaction
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TransactionExists(id))
                {
                    return NotFound("Transaction not found.");
                }
                else
                {
                    throw;
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "PutTransaction failed for TransactionId={TransactionId}", id);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        // DELETE: api/Transactions/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            try
            {
                var transaction = await _context.Transactions.FindAsync(id);
                if (transaction == null)
                {
                    return NotFound("Transaction not found.");
                }

                // HIGH-01: soft-delete — preserve audit trail for financial records
                transaction.IsDeleted = true;
                _context.Transactions.Update(transaction);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Transaction deleted successfully.",
                    DeletedTransactionId = id
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "DeleteTransaction failed for TransactionId={TransactionId}", id);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        private bool TransactionExists(int id)
        {
            return _context.Transactions.Any(e => e.TransactionId == id && !e.IsDeleted);
        }
    }
}

