using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TechShop_API_backend_.Interfaces;
using TechShop_API_backend_.Helpers;
using MongoDB.Driver.Core.Configuration;
using TechShop_API_backend_.Models.Authenticate;

//using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace TechShop_API_backend_.Data
{
    public class AuthenticationRepository 
    {
        private static readonly string _connectionString = Environment.GetEnvironmentVariable("ConnectionString__UserDatabase") ?? throw new InvalidOperationException("Database connection string not configured");

        public AuthenticationRepository()
        {
        }

        // Example of corrected usage of Microsoft.Data.SqlClient.SqlConnection
        public void AddUser(User user)
        {
            using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                string query = "INSERT INTO users (Id, Email, Username, Password, Salt, IsAdmin) VALUES (@Id, @Email, @Username, @Password, @Salt, @IsAdmin)";
                using (var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", user.Id);
                    command.Parameters.AddWithValue("@Email", user.Email);
                    command.Parameters.AddWithValue("@Username", user.Username);
                    command.Parameters.AddWithValue("@Password", user.Password);
                    command.Parameters.AddWithValue("@Salt", user.Salt);
                    command.Parameters.AddWithValue("@IsAdmin", user.IsAdmin);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }
        //public int AssignId()
        //{
        //    using (SqlConnection connection = new SqlConnection(_connectionString))
        //    {
        //        string query = @"SELECT Id FROM userId";
        //        SqlCommand command = new SqlCommand(query, connection);
        //        connection.Open();
        //        SqlDataReader reader = command.ExecuteReader();

        //        if (reader.Read())
        //        {
        //            return reader.GetInt32(0) + 1;
        //        }

        //        return 10000; // Trả về null nếu sai tài khoản/mật khẩu
        //    }
        //}




        // Ensure all other methods use Microsoft.Data.SqlClient.SqlConnection and SqlCommand
    }
// Use this library for SqlConnection and SqlCommand
}