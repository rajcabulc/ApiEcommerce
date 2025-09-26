using System.ComponentModel.DataAnnotations;

namespace ApiEcommerce.Models.Dtos
{
    public class CreateUserDto
    {
        [Required(ErrorMessage = "El campo Name es requerido")]
        public string? Name { get; set; }
        [Required(ErrorMessage = "El campo Username es requerido")]
        public string? Username { get; set; }
        [Required(ErrorMessage = "El campo Password es requerido")]
        public string? Password { get; set; }
        [Required(ErrorMessage = "El campo Role es requerido")]
        public string? Role { get; set; }
    }
}
