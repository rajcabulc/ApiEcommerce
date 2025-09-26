using ApiEcommerce.Data;
using ApiEcommerce.Models;
using ApiEcommerce.Models.Dtos;
using ApiEcommerce.Repository.IRepository;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiEcommerce.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _dbCon;
        private readonly string? secretKey;
        public UserRepository(AppDbContext dbCon, IConfiguration configuration)
        {
            _dbCon = dbCon;
            secretKey = configuration.GetValue<string>("ApiSettings:SecretKey");
        }
        public User? GetUser(int id)
        {
            return _dbCon.Users.FirstOrDefault(u => u.Id == id);
        }

        public ICollection<User> GetUsers()
        {
            return _dbCon.Users.OrderBy(u => u.Username).ToList();
        }

        public bool IsUniqueUser(string username)
        {
            return !_dbCon.Users.Any(u => u.Username.ToLower().Trim() == username.ToLower().Trim());
        }

        // login de usuario
        public async Task<UserLoginResponseDto> Login(UserLoginDto userLoginDto)
        {
            // verificar si el username es nulo o vacio
            if (string.IsNullOrEmpty(userLoginDto.Username))
            {
                return new UserLoginResponseDto()
                {
                    Token = "",
                    User = null,
                    Message = "El Username es requerido"
                };
            }
            // buscar el username en la db y verificar que exista
            var user = _dbCon.Users.FirstOrDefault<User>(u => u.Username.ToLower().Trim() == userLoginDto.Username.ToLower().Trim());
            if(user == null)
            {
                return new UserLoginResponseDto()
                {
                    Token = "",
                    User = null,
                    Message = "Username no Encontrado"
                };
            }

            if(!BCrypt.Net.BCrypt.Verify(userLoginDto.Password, user.Password))
            {
                return new UserLoginResponseDto()
                {
                    Token = "",
                    User = null,
                    Message = "Las Credenciales son Incorrectas"
                };
            }
            // Generando el JWT
            var handlerToken = new JwtSecurityTokenHandler();
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException("SecretKey no esta Configurada");

            var key = Encoding.UTF8.GetBytes(secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("id", user.Id.ToString()),
                    new Claim("username", user.Username),
                    new Claim(ClaimTypes.Role, user.Role ?? string.Empty)
                }
                ),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = handlerToken.CreateToken(tokenDescriptor);

            return new UserLoginResponseDto()
            {
                Token = handlerToken.WriteToken(token),
                User = new UserRegisterDto()
                {
                    Username = user.Username,
                    Name = user.Name,
                    Role = user.Role,
                    Password = user.Password ?? ""
                },
                Message = "Usuario Logeado Correctamente"
            };
        }

        // Registro de un nuevo usuario
        public async Task<User> Register(CreateUserDto createUserDto)
        {
            // encriptando la contrasenia
            var encriptedPass = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);
            // creando objeto de usuario con los datos recibidos
            var user = new User()
            {
                Username = createUserDto.Username ?? "No Username",
                Name = createUserDto.Name,
                Role = createUserDto.Role,
                Password = encriptedPass
            };

            _dbCon.Users.Add(user);
            await _dbCon.SaveChangesAsync();

            return user;
        }
    }
}
