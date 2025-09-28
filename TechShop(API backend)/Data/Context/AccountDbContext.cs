
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechShop_API_backend_.Models;
using System.Security.Cryptography;
using System.Text;
using TechShop_API_backend_.Helpers;

namespace TechShop_API_backend_.Data.Context
{

public class AccountDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        

        public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) { }
    }


}
