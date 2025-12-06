
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TechShop.API.Models;
using TechShop_API_backend_.Helpers;
using TechShop_API_backend_.Models.Authenticate;

namespace TechShop_API_backend_.Data.Context
{

    public class AuthenticateDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserId> UserId { get; set; }

        public DbSet<AuthProvider> AuthProviders { get; set; }

        public DbSet<VerificationCode> VerificationCodes { get; set; }

        public DbSet<UserFcm> UserFcm { get; set; }


        public AuthenticateDbContext(DbContextOptions<AuthenticateDbContext> options) : base(options) { }

    }

}