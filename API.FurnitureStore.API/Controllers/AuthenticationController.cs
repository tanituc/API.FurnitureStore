﻿using API.FurnitureStore.API.Configuration;
using API.FurnitureStore.Shared.Auth;
using API.FurnitureStore.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API.FurnitureStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtConfig _jwtConfig;
        public AuthenticationController(UserManager<IdentityUser> userManager,
            IOptions<JwtConfig> jwtConfig)
        {
            _userManager = userManager;
            _jwtConfig = jwtConfig.Value;
        }
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegsitrationDto request)
        {
            if (!ModelState.IsValid) return BadRequest();
            //Verify if email exits
            var emailExists = await _userManager.FindByEmailAsync(request.EmailAdress);
            if (emailExists != null) return BadRequest(new AuthResult()
            {
                Result = false,
                Errors = new List<string>()
                {
                    "Email already exists"
                }
            });

            //Create User
            var user = new IdentityUser()
            {
                Email = request.EmailAdress,
                UserName = request.EmailAdress
            };
            var isCreated = await _userManager.CreateAsync(user, request.Password);

            if (isCreated.Succeeded)
            {
                var token = GenerateToken(user);
                return Ok(new AuthResult()
                {
                    Result = true,
                    Token = token
                });
            }
            else
            {
                var errors = new List<string>();
                foreach (var err in isCreated.Errors)
                {
                    errors.Add(err.Description);
                }
                return BadRequest(new AuthResult
                {
                    Result = false,
                    Errors = errors
                });
            }
            return BadRequest(new AuthResult
            {
                Result = false,
                Errors = new List<string> { "User coldn't be created" }
            });
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest();

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser == null) return BadRequest(new AuthResult
            {
                Errors = new List<string> { "Invalid Payload" },
                Result = false
            });

            var checkUserAndPass = await _userManager.CheckPasswordAsync(existingUser, request.Password);

            if (!checkUserAndPass)
                return BadRequest(new AuthResult
                {
                    Errors = new List<string> { "Invalid Credentials" },
                    Result = false
                });
            var token = GenerateToken(existingUser);
            return Ok(new AuthResult { Token = token, Result = true });
        }
        private string GenerateToken(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.UTF8.GetBytes(_jwtConfig.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new ClaimsIdentity(new[]
                {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToUniversalTime().ToString())
                })),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);

            return jwtTokenHandler.WriteToken(token);
        }
    }
}