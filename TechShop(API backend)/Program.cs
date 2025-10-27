using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver.Core.Configuration;
using TechShop_API_backend_.Data;
using TechShop_API_backend_.Data.Context;
using TechShop_API_backend_.Models;
using TechShop_API_backend_.Service;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using TechShop_API_backend_.Data.Authenticate;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using TechShop.API.Repositories;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Add custom services
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuthenticationRepository>();
builder.Services.AddScoped<UserDetailRepository>();
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<ReviewRepository>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<VerificationCodeRepository>();
builder.Services.AddScoped<AuthProviderRepository>();    
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<ImageService>();
// Add service configurations
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AuthenticateDbContext>(options =>
    options.UseSqlServer(Environment.GetEnvironmentVariable("ConnectionString__UserDatabase")));
//;





builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()   
            .AllowAnyMethod()  
            .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option =>
{
option.SaveToken = true;
option.RequireHttpsMetadata = false;
option.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
{
    ValidAudience = builder.Configuration["JwtConfig:Audience"],
    ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"])),
    
    ValidateIssuerSigningKey = true,
    ValidateLifetime = true,
    ValidateIssuer = true,
    ValidateAudience = true,

};
});



// ADD SWAGGER WITH JWT AUTHENTICATE
builder.Services.AddSwaggerGen(options =>
{
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    options.AddSecurityDefinition("Bearer", jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});



// ADD RATE LIMITER


builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("authenticate", opt =>
    {
        opt.PermitLimit = 4;
        opt.Window = TimeSpan.FromSeconds(12);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
});





var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    
}
app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();



// Enable rate limiting 
app.UseRateLimiter();
//authenticated rate limited endpoint 
app.MapGet("/api/Authenticate", () => "This endpoint is rate limited")
   .RequireRateLimiting("authenticate"); // Apply specific policy to an endpoint



app.Run();
