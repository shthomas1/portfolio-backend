using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FeedbackApi.Data;
using FeedbackApi.Models;

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
        public async Task<ActionResult<IEnumerable<Feedback>>> GetFeedbacks()
        {
            return await _context.Feedbacks.ToListAsync();
        }
        
        // GET: api/Feedback/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Feedback>> GetFeedback(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            
            if (feedback == null)
            {
                return NotFound();
            }
            
            return feedback;
        }
        
        // POST: api/Feedback
        [HttpPost]
        public async Task<ActionResult<Feedback>> PostFeedback(Feedback feedback)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            feedback.CreatedAt = DateTime.UtcNow; // Set the creation timestamp
            
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetFeedback), new { id = feedback.Id }, feedback);
        }
    }
}