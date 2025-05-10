using Microsoft.EntityFrameworkCore;
using FeedbackApi.Models;

namespace FeedbackApi.Data
{
    public class FeedbackDbContext : DbContext
    {
        public FeedbackDbContext(DbContextOptions<FeedbackDbContext> options) 
            : base(options)
        {
        }
        
        public DbSet<Feedback> Feedbacks { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // You can add any additional configuration here
            base.OnModelCreating(modelBuilder);
        }
    }
}