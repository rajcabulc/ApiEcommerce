using ApiEcommerce.Data;
using ApiEcommerce.Models;
using ApiEcommerce.Repository.IRepository;

namespace ApiEcommerce.Repository
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _dbCon;
        public CategoryRepository(AppDbContext dbCon)
        {
            _dbCon = dbCon;
        }
        public bool CategoryExists(int id)
        {
            return _dbCon.Categories.Any(c => c.Id == id);
        }

        public bool CategoryExists(string name)
        {
            return _dbCon.Categories.Any(c => c.Name.ToLower().Trim() == name.ToLower().Trim());
        }

        public bool CreateCategory(Category category)
        {
            category.CreationDate = DateTime.Now;
            _dbCon.Categories.Add(category);

            return Save();
        }

        public bool DeleteCategory(Category category)
        {
            _dbCon.Categories.Remove(category);

            return Save();
        }

        public ICollection<Category> GetCategories()
        {
            return _dbCon.Categories.OrderBy(c => c.Name).ToList();
        }

        public Category? GetCategory(int id)
        {
            return _dbCon.Categories.FirstOrDefault(c => c.Id == id);
        }

        public bool Save()
        {
            return _dbCon.SaveChanges() >= 0 ? true : false;
        }

        public bool UpdateCategory(Category category)
        {
            category.CreationDate = DateTime.Now;
            _dbCon.Categories.Update(category);

            return Save();
        }
    }
}
