using System;
using System.ComponentModel.DataAnnotations;

namespace FeedbackApi.Models
{
    public class Feedback
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }
        
        [Range(1, 10)]
        public int Rating { get; set; }
        
        [Range(1, 10)]
        public int Usability { get; set; }
        
        [Range(1, 10)]
        public int Design { get; set; }
        
        [Range(1, 10)]
        public int Content { get; set; }
        
        [StringLength(1000)]
        public string Comments { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}