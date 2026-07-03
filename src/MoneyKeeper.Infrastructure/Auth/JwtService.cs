using Microsoft.Extensions.Options;
using MoneyKeeper.Application.Common.Auth;
using MoneyKeeper.Core.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MoneyKeeper.Infrastructure.Auth
{
    public class JwtService : IJwtService
    {
        private readonly IOptions<JwtOptions> _options;

        public JwtService(IOptions<JwtOptions> options)
        {
            _options = options;
        }

        public string Generate(User user)
        {
            List<Claim> claims = new List<Claim>()
            {
                new Claim("id", user.Id.ToString())
            };

            JwtSecurityToken token = new JwtSecurityToken(
                expires: DateTime.UtcNow.Add(_options.Value.Expires),
                claims: claims,
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(_options.Value.SecretKey)),
                    SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
