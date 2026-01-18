using AutoMapper;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Application.Mappers;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Category mappings
        CreateMap<Category, CategoryDto>()
            .ForMember(dest => dest.ParentCategoryName, opt => opt.MapFrom(src => src.ParentCategory != null ? src.ParentCategory.Name : null))
            .ForMember(dest => dest.SubCategories, opt => opt.MapFrom(src => src.SubCategories.Where(sc => sc.IsActive)));
        CreateMap<CreateCategoryDto, Category>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => GenerateSlug(src.Name)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));
        CreateMap<UpdateCategoryDto, Category>()
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => GenerateSlug(src.Name)))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Product mappings
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name));
        CreateMap<Product, ProductListDto>()
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ForMember(dest => dest.PrimaryImageUrl, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.ImageUrl) ? src.ImageUrl :
                src.Images.FirstOrDefault(i => i.IsPrimary) != null ? src.Images.FirstOrDefault(i => i.IsPrimary)!.ImageUrl : 
                src.Images.FirstOrDefault() != null ? src.Images.FirstOrDefault()!.ImageUrl : null));
        CreateMap<CreateProductDto, Product>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => GenerateSlug(src.Name)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));
        CreateMap<UpdateProductDto, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => GenerateSlug(src.Name)))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()) // Set manually in service
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ImageUrl, opt => opt.Ignore()) // Don't overwrite image URL
            .ForMember(dest => dest.DetailImageUrl, opt => opt.Ignore()) // Don't overwrite detail image URL
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.Brand, opt => opt.Ignore())
            .ForMember(dest => dest.AttributeValues, opt => opt.Ignore())
            .ForMember(dest => dest.Specifications, opt => opt.Ignore())
            .ForMember(dest => dest.CartItems, opt => opt.Ignore())
            .ForMember(dest => dest.Favorites, opt => opt.Ignore());

        // ProductImage mappings
        CreateMap<ProductImage, ProductImageDto>();

        // User mappings
        CreateMap<User, UserDto>();
        CreateMap<CreateUserDto, User>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()); // Will be set by service
        CreateMap<UpdateUserDto, User>()
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Filter mappings
        CreateMap<Filter, FilterDto>();
        CreateMap<CreateFilterDto, Filter>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => GenerateSlug(src.Name)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Options, opt => opt.Ignore()); // Will be mapped manually
        CreateMap<UpdateFilterDto, Filter>()
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => GenerateSlug(src.Name)))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // FilterOption mappings
        CreateMap<FilterOption, FilterOptionDto>();
        CreateMap<CreateFilterOptionDto, FilterOption>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
        CreateMap<UpdateFilterOptionDto, FilterOption>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // ProductAttributeValue mappings
        CreateMap<ProductAttributeValue, ProductAttributeValueDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : ""))
            .ForMember(dest => dest.FilterName, opt => opt.MapFrom(src => src.Filter != null ? src.Filter.Name : ""))
            .ForMember(dest => dest.FilterOptionDisplayName, opt => opt.MapFrom(src => src.FilterOption != null ? src.FilterOption.DisplayName : null));

        // Banner mappings
        CreateMap<Banner, BannerDto>();
        CreateMap<CreateBannerDto, Banner>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.ImageUrl, opt => opt.Ignore()); // Will be set by service
        CreateMap<CreateBannerWithImageDto, Banner>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.ImageUrl, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.MobileImageUrl, opt => opt.Ignore()); // Will be set by service
        CreateMap<UpdateBannerDto, Banner>()
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Favorite mappings
        CreateMap<UserFavorite, FavoriteDto>()
            .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product));
        CreateMap<CreateFavoriteDto, UserFavorite>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore());

        // DownloadableFile mappings
        CreateMap<DownloadableFile, DownloadableFileDto>()
            .ForMember(dest => dest.CreatedByUserName, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.UpdatedByUserName, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.DownloadUrl, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.FileSizeFormatted, opt => opt.Ignore()); // Will be set by service

        // ProductPdf mappings
        CreateMap<ProductPdf, ProductPdfDto>()
            .ForMember(dest => dest.ProductName, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.CreatedByUserName, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.UpdatedByUserName, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.DownloadUrl, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.FileSizeFormatted, opt => opt.Ignore()); // Will be set by service

        // Brand mappings
        CreateMap<Brand, BrandDto>();
        CreateMap<CreateBrandDto, Brand>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => GenerateSlug(src.Name)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));
        CreateMap<UpdateBrandDto, Brand>()
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => GenerateSlug(src.Name)))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "and")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("!", "")
            .Replace("?", "");
    }
}
