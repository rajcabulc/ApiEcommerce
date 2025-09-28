using ApiEcommerce.Models;
using ApiEcommerce.Models.Dtos;
using ApiEcommerce.Models.Dtos.User;
using Mapster;

namespace ApiEcommerce.Mapping
{
    public static class MapsterConfig
    {
        public static void RegisterMappings()
        {
            TypeAdapterConfig<Category, CategoryDto>.NewConfig();
            TypeAdapterConfig<Category, CreateCategoryDto>.NewConfig();

            TypeAdapterConfig<Product, ProductDto>
                .NewConfig()
                .Map(dest => dest.CategoryName, src => src.Category.Name);
            TypeAdapterConfig<Product, CreateProductDto>.NewConfig();
            TypeAdapterConfig<Product, UpdateProductDto>.NewConfig();

            TypeAdapterConfig<User, UserDto>.NewConfig();
            TypeAdapterConfig<User, CreateUserDto>.NewConfig();
            TypeAdapterConfig<User, UserLoginDto>.NewConfig();
            TypeAdapterConfig<User, UserLoginResponseDto>.NewConfig();
            TypeAdapterConfig<ApplicationUser, UserDataDto>.NewConfig();
            TypeAdapterConfig<ApplicationUser, UserDto>.NewConfig();
        }
    }
}
