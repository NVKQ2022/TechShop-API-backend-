
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TechShop_API_backend_.Helpers;
using TechShop_API_backend_.Models.Authenticate;

namespace TechShop_API_backend_.Data.Context
{

    public class AccountDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserId> UserId { get; set; }

        public DbSet<AuthProvider> AuthProviders { get; set; }

        public DbSet<OTPCode> OTPCodes { get; set; }

        public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) { }
    }


}
