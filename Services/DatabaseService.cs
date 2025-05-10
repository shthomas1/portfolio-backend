using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using portfolio_backend.Models;

namespace portfolio_backend.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            // Convert the connection URL format to MySql connection string
            var uri = new Uri(connectionString.Replace("mysql://", "http://"));
            var userInfo = uri.UserInfo.Split(':');
            var builder = new MySqlConnectionStringBuilder
            {
                Server = uri.Host,
                Port = (uint)uri.Port,
                Database = uri.AbsolutePath.Trim('/'),
                UserID = userInfo[0],
                Password = userInfo[1],
                SslMode = MySqlSslMode.None,
                AllowPublicKeyRetrieval = true
            };

            _connectionString = builder.ConnectionString;
            InitializeDatabase().Wait();
        }

        private async Task InitializeDatabase()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var createTableCmd = @"
                    CREATE TABLE IF NOT EXISTS Feedback (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        Name VARCHAR(100) NOT NULL,
                        Email VARCHAR(100) NOT NULL,
                        Rating INT NOT NULL,
                        Usability INT NOT NULL,
                        Design INT NOT NULL,
                        Content INT NOT NULL,
                        Comments TEXT,
                        SubmittedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );";

                using (var command = new MySqlCommand(createTableCmd, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<bool> SaveFeedback(Feedback feedback)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var insertCmd = @"
                    INSERT INTO Feedback (Name, Email, Rating, Usability, Design, Content, Comments)
                    VALUES (@name, @email, @rating, @usability, @design, @content, @comments);";

                using (var command = new MySqlCommand(insertCmd, connection))
                {
                    command.Parameters.AddWithValue("@name", feedback.Name);
                    command.Parameters.AddWithValue("@email", feedback.Email);
                    command.Parameters.AddWithValue("@rating", feedback.Rating);
                    command.Parameters.AddWithValue("@usability", feedback.Usability);
                    command.Parameters.AddWithValue("@design", feedback.Design);
                    command.Parameters.AddWithValue("@content", feedback.Content);
                    command.Parameters.AddWithValue("@comments", feedback.Comments ?? string.Empty);

                    var result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
        }

        public async Task<List<Feedback>> GetAllFeedback()
        {
            var feedbackList = new List<Feedback>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var selectCmd = "SELECT * FROM Feedback ORDER BY SubmittedAt DESC;";

                using (var command = new MySqlCommand(selectCmd, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            feedbackList.Add(new Feedback
                            {
                                Id = reader.GetInt32(0),                // Id column (first column)
                                Name = reader.GetString(1),             // Name column (second column)
                                Email = reader.GetString(2),            // Email column (third column)
                                Rating = reader.GetInt32(3),            // Rating column (fourth column)
                                Usability = reader.GetInt32(4),         // Usability column (fifth column)
                                Design = reader.GetInt32(5),            // Design column (sixth column)
                                Content = reader.GetInt32(6),           // Content column (seventh column)
                                Comments = reader.IsDBNull(7) ? null : reader.GetString(7), // Comments column (eighth column)
                                SubmittedAt = reader.GetDateTime(8)     // SubmittedAt column (ninth column)
                            });
                        }
                    }
                }
            }

            return feedbackList;
        }
    }
}