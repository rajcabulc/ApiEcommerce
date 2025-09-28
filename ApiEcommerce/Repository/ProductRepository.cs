using ApiEcommerce.Data;
using ApiEcommerce.Models;
using ApiEcommerce.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace ApiEcommerce.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _dbCon;
        public ProductRepository(AppDbContext dbCon)
        {
            _dbCon = dbCon;
        }
        public bool BuyProduct(string name, int quantity)
        {
            if (string.IsNullOrEmpty(name) || quantity <= 0)
                return false;

            var product = _dbCon.Products.FirstOrDefault(p => p.Name.ToLower().Trim() == name.ToLower().Trim());
            if (product == null || product.Stock < quantity)
                return false;

            product.Stock -= quantity;
            _dbCon.Products.Update(product);

            return Save();
        }

        public bool CreateProduct(Product product)
        {
            if (product == null)
                return false;

            product.CreationDate = DateTime.Now;
            product.UpdateDate = DateTime.Now;
            _dbCon.Products.Add(product);

            return Save();
        }

        public bool DeleteProduct(Product product)
        {
            if (product == null)
                return false;

            _dbCon.Products.Remove(product);

            return Save();
        }

        public Product? GetProduct(int productId)
        {
            if (productId <= 0)
                return null;

            return _dbCon.Products.Include(p => p.Category).FirstOrDefault(p => p.ProductId == productId);
        }

        public ICollection<Product> GetProducts()
        {
            return _dbCon.Products.Include(p => p.Category).OrderBy(p => p.Name).ToList();
        }

        public ICollection<Product> GetProductsForCategory(int categoryId)
        {
            if (categoryId <= 0)
                return new List<Product>(); // lista vacia

            return _dbCon.Products.Include(p => p.Category).Where(p => p.CategoryId == categoryId).OrderBy(p => p.Name).ToList();
        }

        public ICollection<Product> GetProductsInPages(int pageNumber, int pageSize)
        {
            return _dbCon.Products.OrderBy(p => p.ProductId).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        }

        public int GetTotalProducts()
        {
            return _dbCon.Products.Count();
        }

        public bool ProductExists(int id)
        {
            if (id <= 0)
                return false;

            return _dbCon.Products.Any(p => p.ProductId == id);
        }

        public bool ProductExists(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return _dbCon.Products.Any(p => p.Name.ToLower().Trim() == name.ToLower().Trim());
        }

        public bool Save()
        {
            return _dbCon.SaveChanges() >= 0;
        }

        public ICollection<Product> SearchProducts(string searchTerm)
        {
            IQueryable<Product> query = _dbCon.Products;
            var searchTermLowered = searchTerm.ToLower().Trim();

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Include(p => p.Category).Where(p => p.Name.ToLower().Trim().Contains(searchTermLowered) ||
                p.Description.ToLower().Trim().Contains(searchTermLowered));

            return query.OrderBy(p => p.Name).ToList();
        }

        public bool UpdateProduct(Product product)
        {
            if (product == null)
                return false;

            product.UpdateDate = DateTime.Now;
            _dbCon.Products.Update(product);

            return Save();
        }
    }
}
