using ApiEcommerce.Models;

namespace ApiEcommerce.Repository.IRepository
{
    public interface IProductRepository
    {
        //
        //        → Devuelve todos los productos
        //          en ICollection del tipo Product.
        ICollection<Product> GetProducts();
        ICollection<Product> GetProductsInPages(int pageNumber, int pageSize);
        int GetTotalProducts();
        //        → Recibe un categoryId y devuelve los productos
        //          de esa categoría en ICollection del tipo Product.
        ICollection<Product> GetProductsForCategory(int categoryId);
        //        → Recibe un nombre y devuelve los productos
        //          que coincidan en ICollection del tipo Product.
        ICollection<Product> SearchProducts(string searchTerm);
        //        → Recibe un id y devuelve un solo objeto Product
        //          o null si no se encuentra.
        Product? GetProduct(int productId);
        //        → Recibe el nombre del producto y una cantidad,
        //          y devuelve un bool indicando si la compra fue exitosa.
        bool BuyProduct(string name, int quantity);
        //        → Recibe un id y devuelve un bool
        //          indicando si existe el producto.
        bool ProductExists(int id);
        //        → Recibe un nombre y devuelve un bool
        //          indicando si existe el producto.
        bool ProductExists(string name);
        //        → Recibe un objeto Product 
        //          y devuelve un bool indicando si la creación fue exitosa.
        bool CreateProduct(Product product);
        //        → Recibe un objeto Product
        //          y devuelve un bool indicando si la actualización fue exitosa.
        bool UpdateProduct(Product product);
        //        → Recibe un objeto Product
        //          y devuelve un bool indicando si la eliminación fue exitosa.
        bool DeleteProduct(Product product);
        //        → Devuelve un bool indicando
        //          si los cambios se guardaron correctamente.
        bool Save();
    }
}
