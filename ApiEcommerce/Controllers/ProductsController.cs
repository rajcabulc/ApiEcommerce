using ApiEcommerce.Models;
using ApiEcommerce.Models.Dtos;
using ApiEcommerce.Models.Dtos.Responses;
using ApiEcommerce.Repository.IRepository;
using Asp.Versioning;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiEcommerce.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductsController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }
        // GET listado productos
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetProducts()
        {
            var products = _productRepository.GetProducts();
            var productsDto = products.Adapt<List<ProductDto>>();
            return Ok(productsDto);
        }

        // GET by Id
        [AllowAnonymous]
        [HttpGet("{productId:int}", Name = "GetProduct")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetProduct(int productId)
        {
            var product = _productRepository.GetProduct(productId);
            if (product == null)
                return NotFound($"El Producto Id {productId} no existe!.");

            var productDto = product.Adapt<ProductDto>();
            return Ok(productDto);
        }

        // GET by Id
        [AllowAnonymous]
        [HttpGet("Paged", Name = "GetProductsInPage")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetProductsInPage([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            if (pageNumber < 1 || pageSize < 1)
                return BadRequest("Los parametros de paginacion no son validos.");

            var totalProducts = _productRepository.GetTotalProducts();
            var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
            if (pageNumber > totalPages)
                return NotFound("No hay mas paginas disponibles.");

            var products = _productRepository.GetProductsInPages(pageNumber, pageSize);
            var productDto = products.Adapt<List<ProductDto>>();

            var paginationResponse = new PaginationResponse<ProductDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                Items = productDto
            };

            return Ok(paginationResponse);
        }

        // POST
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult CreateProduct([FromForm] CreateProductDto createProductDto)
        {
            if (createProductDto == null)
                return BadRequest(ModelState);

            if (_productRepository.ProductExists(createProductDto.Name))
            {
                ModelState.AddModelError("CustomError", "El Producto ya existe!");
                return BadRequest(ModelState);
            }
            if (!_categoryRepository.CategoryExists(createProductDto.CategoryId))
            {
                ModelState.AddModelError("CustomError", $"La Categoria con el {createProductDto.CategoryId} no existe!");
                return BadRequest(ModelState);
            }

            var product = createProductDto.Adapt<Product>();
            // agregando imagen
            if(createProductDto.Image != null)
                UploadProductImage(createProductDto, product);
            else
                product.ImgUrl = "https://placehold.co/300x300";

            if (!_productRepository.CreateProduct(product))
            {
                ModelState.AddModelError("CustomError", $"Algo salio mal al guardar el registro {product.Name}");
                return StatusCode(500, ModelState);
            }

            var createdProduct = _productRepository.GetProduct(product.ProductId);
            var productDto = createdProduct.Adapt<ProductDto>();

            return CreatedAtRoute("GetProduct", new { productId = product.ProductId }, productDto);
        }

        // GET by Id
        [HttpGet("searchProductByCategory/{categoryId:int}", Name = "GetProductForCategory")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetProductsForCategory(int categoryId)
        {
            var products = _productRepository.GetProductsForCategory(categoryId);
            if (products.Count == 0)
                return NotFound($"Los productos con la Categoria {categoryId} no existen!.");

            var productsDto = products.Adapt<List<ProductDto>>();
            return Ok(productsDto);
        }

        // GET by Nombre o Descripcion
        [HttpGet("searchProductByNameDescription/{searchTerm}", Name = "SearchProducts")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult SearchProducts(string searchTerm)
        {
            var products = _productRepository.SearchProducts(searchTerm);
            if (products.Count == 0)
                return NotFound($"Los productos con el nombre {searchTerm} no existen!.");

            var productsDto = products.Adapt<List<ProductDto>>();
            return Ok(productsDto);
        }

        // PATCH 
        [HttpPatch("buyProduct/{name}/{quantity:int}", Name = "BuyProduct")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult BuyProduct(string name, int quantity)
        {
            if (string.IsNullOrWhiteSpace(name) || quantity <= 0)
                return BadRequest("El nombre del producto o la Cantidad no son validos.");

            var foundProducts = _productRepository.ProductExists(name);
            if (!foundProducts)
                return NotFound($"El producto con el nombre {name} no existe.");

            if (!_productRepository.BuyProduct(name, quantity))
            {
                ModelState.AddModelError("CustomError", $"No se pudo comprar el producto {name} o la cantidad solicitada es mayor al stock disponible.");
                return BadRequest(ModelState);
            }

            var units = quantity == 1 ? "unidad" : "unidades";
            return Ok($"Se compro {quantity} {units} del producto '{name}'");
        }

        // POST
        [HttpPut("{productId:int}", Name = "UpdateProduct")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult UpdateProduct(int productId, [FromForm] UpdateProductDto updateProductDto)
        {
            if (updateProductDto == null)
                return BadRequest(ModelState);

            if (!_productRepository.ProductExists(productId))
            {
                ModelState.AddModelError("CustomError", "El Producto no existe!");
                return BadRequest(ModelState);
            }
            if (!_categoryRepository.CategoryExists(updateProductDto.CategoryId))
            {
                ModelState.AddModelError("CustomError", $"La Categoria con el {updateProductDto.CategoryId} no existe!");
                return BadRequest(ModelState);
            }

            var product = updateProductDto.Adapt<Product>();
            product.ProductId = productId;
            // agregando imagen
            if(updateProductDto.Image != null)
                UploadProductImage(updateProductDto, product);
            else
                product.ImgUrl = "https://placehold.co/300x300";

            if (!_productRepository.UpdateProduct(product))
            {
                ModelState.AddModelError("CustomError", $"Algo salio mal al actualizar el registro {product.Name}");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }

        private void UploadProductImage(dynamic productDto, Product product)
        {
            string fileName = product.ProductId + Guid.NewGuid().ToString() + Path.GetExtension(productDto.Image.FileName); // generar un nombre unico
            var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ProductsImages"); // carpeta donde se guardaran las imagenes
            if (!Directory.Exists(imagesFolder)) // si la carpeta no existe, crearla
                Directory.CreateDirectory(imagesFolder);
            var filePath = Path.Combine(imagesFolder, fileName); // ruta completa del archivo
            FileInfo file = new FileInfo(filePath);
            if (file.Exists) // si el archivo ya existe, eliminarlo
                file.Delete();

            using var fileStream = new FileStream(filePath, FileMode.Create); // crear el archivo
            productDto.Image.CopyTo(fileStream); // copiar la imagen al archivo

            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
            product.ImgUrl = $"{baseUrl}/ProductsImages/{fileName}"; // guardar la url completa en la base de datos
            product.ImgUrlLocal = filePath; // guardar la ruta local en la base de datos
        }

        // DELETE
        [HttpDelete("{productId:int}", Name = "DeleteProduct")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult DeleteProduct(int productId)
        {
            if (productId == 0)
                return BadRequest(ModelState);

            var product = _productRepository.GetProduct(productId);
            if (product == null)
                return NotFound($"El Producto Id {productId} no existe!.");

            if (!_productRepository.DeleteProduct(product))
            {
                ModelState.AddModelError("CustomError", $"Algo salio mal al eliminar el registro {product.Name}");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }
    }
}
