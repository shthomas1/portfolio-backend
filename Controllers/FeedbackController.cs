using Microsoft.AspNetCore.Mvc;
using portfolio_backend.Models;
using portfolio_backend.Services;

namespace portfolio_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(DatabaseService databaseService, ILogger<FeedbackController> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback([FromBody] Feedback feedback)
        {
            if (string.IsNullOrEmpty(feedback.Name) || string.IsNullOrEmpty(feedback.Email))
            {
                return BadRequest(new { message = "Name and Email are required" });
            }

            try
            {
                var success = await _databaseService.SaveFeedback(feedback);
                
                if (success)
                {
                    return Ok(new { message = "Feedback submitted successfully!" });
                }
                
                return StatusCode(500, new { message = "Failed to save feedback." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving feedback");
                return StatusCode(500, new { message = "An error occurred while saving feedback." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFeedback()
        {
            try
            {
                var feedbackList = await _databaseService.GetAllFeedback();
                return Ok(feedbackList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback");
                return StatusCode(500, new { message = "An error occurred while retrieving feedback." });
            }
        }
    }
}