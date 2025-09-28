﻿using ApiEcommerce.Data;
using ApiEcommerce.Models;
using ApiEcommerce.Models.Dtos;
using ApiEcommerce.Models.Dtos.User;
using ApiEcommerce.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mapster;

namespace ApiEcommerce.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _dbCon;
        private readonly string? secretKey;
        private readonly UserManager<ApplicationUser> _userManager; //
        private readonly RoleManager<IdentityRole> _roleManager;
        public UserRepository(AppDbContext dbCon, IConfiguration configuration, UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager)
        {
            _dbCon = dbCon;
            secretKey = configuration.GetValue<string>("ApiSettings:SecretKey");
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public ApplicationUser? GetUser(string id)
        {
            return _dbCon.ApplicationUsers.FirstOrDefault(u => u.Id == id);
        }

        public ICollection<ApplicationUser> GetUsers()
        {
            return _dbCon.ApplicationUsers.OrderBy(u => u.UserName).ToList();
        }

        public bool IsUniqueUser(string username)
        {
            return !_dbCon.Users.Any(u => u.Username.ToLower().Trim() == username.ToLower().Trim());
        }

        // login de usuario
        public async Task<UserLoginResponseDto> Login(UserLoginDto userLoginDto)
        {
            if (string.IsNullOrEmpty(userLoginDto.Username))
            {
                return new UserLoginResponseDto()
                {
                    Token = "",
                    User = null,
                    Message = "El Username es requerido"
                };
            }
            var user = await _dbCon.ApplicationUsers.FirstOrDefaultAsync<ApplicationUser>(u => u.UserName != null && u.UserName.ToLower().Trim() == userLoginDto.Username.ToLower().Trim());
            if(user == null)
            {
                return new UserLoginResponseDto()
                {
                    Token = "",
                    User = null,
                    Message = "Username no Encontrado"
                };
            }

            if(userLoginDto.Password == null)
            {
                return new UserLoginResponseDto()
                {
                    Token = "",
                    User = null,
                    Message = "Password Requerido"
                };
            }
            bool isValid = await _userManager.CheckPasswordAsync(user, userLoginDto.Password);
            if(!isValid)
            {
                return new UserLoginResponseDto()
                {
                    Token = "",
                    User = null,
                    Message = "Las Credenciales son Incorrectas"
                };
            }
            var handlerToken = new JwtSecurityTokenHandler();
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException("SecretKey no esta Configurada");

            var roles = await _userManager.GetRolesAsync(user);
            var key = Encoding.UTF8.GetBytes(secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("id", user.Id.ToString()),
                    new Claim("username", user.UserName ?? string.Empty),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault() ?? string.Empty)
                }
                ),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = handlerToken.CreateToken(tokenDescriptor);

            return new UserLoginResponseDto()
            {
                Token = handlerToken.WriteToken(token),
                User = user.Adapt<UserDataDto>(),
                Message = "Usuario Logeado Correctamente"
            };
        }

        // Registro de un nuevo usuario
        public async Task<UserDataDto> Register(CreateUserDto createUserDto)
        {
            if (string.IsNullOrEmpty(createUserDto.Username))
                throw new ArgumentException("El Username es requerido");

            if (createUserDto.Password == null)
                throw new ArgumentException("El Password es requerido");

            var user = new ApplicationUser()
            {
                UserName = createUserDto.Username,
                Email = createUserDto.Username,
                NormalizedEmail = createUserDto.Username.ToUpper(),
                Name = createUserDto.Name
            };

            var result = await _userManager.CreateAsync(user, createUserDto.Password);
            if (result.Succeeded)
            {
                var userRole = createUserDto.Role ?? "User";
                var roleExists = await _roleManager.RoleExistsAsync(userRole);
                if (!roleExists)
                {
                    var identityRole = new IdentityRole(userRole);
                    await _roleManager.CreateAsync(identityRole);
                }
                await _userManager.AddToRoleAsync(user, userRole);
                var createdUser = _dbCon.ApplicationUsers.FirstOrDefault(u => u.UserName == createUserDto.Username);

                return createdUser.Adapt<UserDataDto>();
            }
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ApplicationException($"No se puedo realizar el Registro: {errors}");
        }
    }
}
