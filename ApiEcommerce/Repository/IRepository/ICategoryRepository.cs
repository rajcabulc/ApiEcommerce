using ApiEcommerce.Models;

namespace ApiEcommerce.Repository.IRepository
{
    public interface ICategoryRepository
    {
        // CRUD operations
        ICollection<Category> GetCategories(); // obtener todas las categorias
        Category? GetCategory(int id); // obtener una categoria por id
        bool CategoryExists(int id); // verificar si una categoria existe por medio del id
        bool CategoryExists(string name); // verificar si una categoria existe por medio del nombre
        bool CreateCategory(Category category); // crear
        bool UpdateCategory(Category category); // actualizar
        bool DeleteCategory(Category category); // eliminar
        bool Save(); // guardar cambios
    }
}
