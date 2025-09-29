using TechShop_API_backend_.Data.Context;
using TechShop_API_backend_.Models.Api;
using TechShop_API_backend_.Helpers;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using TechShop_API_backend_.Models;
using TechShop_API_backend_.Data;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MongoDB.Driver.Linq;
namespace TechShop_API_backend_.Service


{
    public class JwtService
    {
        private readonly IConfiguration _configuration;
        private readonly UserRepository userRepository;
        private byte[] key ;
        private string issuer;
        private string audience;
        int expireMinutes;
        
        public JwtService(IConfiguration configuration, UserRepository userRepository)
        {
            _configuration = configuration;
            this.userRepository = userRepository;

            key = Encoding.ASCII.GetBytes(_configuration["JWT:Key"]);
            issuer = _configuration["JwtConfig:Issuer"];
            audience = _configuration["JwtConfig:Audience"];
            expireMinutes = _configuration.GetValue<int>("JwtConfig:ExpireMinutes");
            
                /*int.Parse(DateTime.UtcNow.AddMinutes(_configuration["Jwt:ExpireMinutes"] != null? Convert.ToDouble(_configuration["Jwt:ExpireMinutes"]): 30));*/ 



        }

        public async Task<LoginResponse>? Authenticate(LoginRequest? request) // returns null if authentication fails ( wrong username or password)  
        {
            if(request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return null;
            }
            var user = await userRepository.GetUserByUsernameAsync(request.Username);
            if (user == null|| !SecurityHelper.VerifyPassword(request.Password, user.Salt, user.Password))
            {
                return null;
            }
           

            return new LoginResponse 
            { 
                Token = GenerateToken(user), 
                IsAdmin = user.IsAdmin , 
                UserId = user.Id, 
                Username = user.Username, 
                ExpiresIn =  expireMinutes*60
                /*(int)DateTime.UtcNow.AddMinutes(expireMinutes).Subtract(DateTime.UtcNow).TotalSeconds*/
            };
        }


        //public string GenerateToken(User user)
        //{
        //    // Generate JWT token
        //    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        //    var key = System.Text.Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
        //    tokenHandler.ValidateToken("", new TokenValidationParameters
        //    {
        //        ValidateIssuerSigningKey = true,
        //        IssuerSigningKey = new SymmetricSecurityKey(key),
        //        ValidateIssuer = true,
        //        ValidateAudience = true,
        //        ClockSkew = TimeSpan.Zero
        //    }, out var validatedToken);

        //    // Token Description
        //    var tokenDescription = new SecurityTokenDescriptor
        //    {
        //        Subject = new System.Security.Claims.ClaimsIdentity(new[]
        //        {
        //            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        //            new Claim(ClaimTypes.Name, user.Username),
        //            new Claim(ClaimTypes.Role, (user.IsAdmin?"Admin":"User")),

        //        }),
        //        Issuer = _configuration["Jwt:Issuer"],
        //        Audience = _configuration["Jwt:Audience"],
        //        Expires = DateTime.UtcNow.AddMinutes(_configuration["Jwt:ExpireMinutes"] != null ? Convert.ToDouble(_configuration["Jwt:ExpireMinutes"]) : 30),
        //        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        //    };


        //    var token = tokenHandler.CreateToken(tokenDescription);
        //    return tokenHandler.WriteToken(token);
        //}


        public string GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescription = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
        }),
                Issuer = issuer,
                Audience = audience,
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(expireMinutes), // calculate at runtime
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescription);
            return tokenHandler.WriteToken(token);
        }





    }
}
