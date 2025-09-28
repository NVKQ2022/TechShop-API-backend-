using TechShop_API_backend_.Data.Context;
using TechShop_API_backend_.Models.Api;
using TechShop_API_backend_.Helpers;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using TechShop_API_backend_.Models;
using TechShop_API_backend_.Data;
using System.Security.Claims;
namespace TechShop_API_backend_.Service


{
    public class JwtService
    {
        private readonly IConfiguration _configuration;
        private readonly UserRepository userRepository;
        public JwtService(IConfiguration configuration, UserRepository userRepository)
        {
            _configuration = configuration;
            this.userRepository = userRepository;
        }

        public async Task<LoginResponse>? Authenticate(LoginRequest request) // returns null if authentication fails ( wrong username or password)  
        {
            if(request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return null;
            }
            var user = await userRepository.GetUserByUsernameAsync(request.Username);
            if (user == null || SecurityHelper.VerifyPassword(request.Password, user.Password, user.Salt))
            {
                return null;
            }
            
            return new LoginResponse { Token = GenerateToken(user), IsAdmin = user.IsAdmin };
        }


        public string GenerateToken(User user)
        {
            // Generate JWT token
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = System.Text.Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            tokenHandler.ValidateToken("", new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            // Token Description
            var tokenDescription = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, (user.IsAdmin?"Admin":"User")),

                }),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                Expires = DateTime.UtcNow.AddMinutes(_configuration["Jwt:ExpireMinutes"] != null ? Convert.ToDouble(_configuration["Jwt:ExpireMinutes"]) : 30),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };


            var token = tokenHandler.CreateToken(tokenDescription);
            return tokenHandler.WriteToken(token);
        }
    }
}
