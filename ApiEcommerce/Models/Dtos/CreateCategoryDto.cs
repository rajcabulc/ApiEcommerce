using System.ComponentModel.DataAnnotations;

namespace ApiEcommerce.Models.Dtos
{
    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MaxLength(50, ErrorMessage = "El nombre no debe exceder los 50 caracteres")]
        [MinLength(3, ErrorMessage = "El nombre no debe tener menos de 3 caracteres")]
        public string Name { get; set; } = string.Empty;
    }
}
