using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FeedbackApi.Data;
using FeedbackApi.Models;
using MySqlConnector;

namespace FeedbackApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly FeedbackDbContext _context;

        public FeedbackController(FeedbackDbContext context)
        {
            _context = context;
        }

        // GET: api/Feedback
        [HttpGet]
        public async Task<ActionResult> GetFeedbacks()
        {
            try
            {
                Console.WriteLine("Attempting to get feedbacks from database...");
                var feedbacks = await _context.Feedbacks.ToListAsync();
                Console.WriteLine($"Successfully retrieved {feedbacks.Count} feedbacks");
                return Ok(feedbacks);
            }
            catch (MySqlException ex)
            {
                // Specific MySQL errors
                Console.WriteLine($"MySQL Error in GetFeedbacks: {ex.Message}");
                return StatusCode(500, new
                {
                    error = "MySQL Error",
                    message = ex.Message,
                    number = ex.Number,
                    sqlState = ex.SqlState,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                // General errors
                Console.WriteLine($"ERROR in GetFeedbacks: {ex.GetType().Name}");
                Console.WriteLine($"Error message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception type: {ex.InnerException.GetType().Name}");
                    Console.WriteLine($"Inner exception message: {ex.InnerException.Message}");
                }

                return StatusCode(500, new
                {
                    error = "Database Error",
                    message = ex.Message,
                    type = ex.GetType().Name,
                    innerMessage = ex.InnerException?.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // GET: api/Feedback/test
        [HttpGet("test")]
        public ActionResult TestDatabase()
        {
            try
            {
                Console.WriteLine("Testing database connection...");
                var canConnect = _context.Database.CanConnect();
                Console.WriteLine($"Database connection test result: {(canConnect ? "Connected" : "Cannot connect")}");

                return Ok(new
                {
                    databaseConnection = canConnect ? "Connected" : "Cannot connect",
                    connectionString = _context.Database.GetConnectionString()?.Split(';')[0] ?? "Not found",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database test error: {ex.Message}");
                return StatusCode(500, new
                {
                    error = "Database test failed",
                    message = ex.Message,
                    innerMessage = ex.InnerException?.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // GET: api/Feedback/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Feedback>> GetFeedback(int id)
        {
            try
            {
                var feedback = await _context.Feedbacks.FindAsync(id);

                if (feedback == null)
                {
                    return NotFound();
                }

                return feedback;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetFeedback({id}): {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/Feedback
        [HttpPost]
        public async Task<ActionResult<Feedback>> PostFeedback(Feedback feedback)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                feedback.CreatedAt = DateTime.UtcNow;

                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                // Return a simple Ok response with the feedback object
                return Ok(feedback);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PostFeedback: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { error = ex.Message, innerError = ex.InnerException?.Message });
            }
        }
    }
}