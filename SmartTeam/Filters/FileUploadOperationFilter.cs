using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using SmartTeam.Application.DTOs;

namespace SmartTeam.Filters;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasFileParameter = context.MethodInfo.GetParameters()
            .Any(p => p.ParameterType == typeof(IFormFile) || 
                     p.ParameterType == typeof(IFormFileCollection) ||
                     p.ParameterType == typeof(IEnumerable<IFormFile>));

        if (!hasFileParameter) return;

        var hasFormDataConsumes = context.MethodInfo.GetCustomAttributes<ConsumesAttribute>()
            .Any(attr => attr.ContentTypes.Contains("multipart/form-data"));

        if (!hasFormDataConsumes) return;

        // Special handling for CreateProductWithImage endpoint
        if (context.MethodInfo.Name == "CreateProductWithImage")
        {
            // Find the productData parameter in the request body
            if (operation.RequestBody?.Content?.ContainsKey("multipart/form-data") == true)
            {
                var schema = operation.RequestBody.Content["multipart/form-data"].Schema;
                if (schema?.Properties?.ContainsKey("productData") == true)
                {
                    // Replace the productData string schema with CreateProductWithImageDto schema
                    var productDataSchema = context.SchemaGenerator.GenerateSchema(typeof(CreateProductWithImageDto), context.SchemaRepository);
                    schema.Properties["productData"] = new OpenApiSchema
                    {
                        Type = "string",
                        Description = "JSON string containing product data",
                        Example = new Microsoft.OpenApi.Any.OpenApiString(
                            @"{
  ""name"": ""iPhone 15 Pro Max"",
  ""description"": ""Latest iPhone with A17 Pro chip"",
  ""shortDescription"": ""Premium smartphone"",
  ""sku"": ""IPH15PM-256-TIT"",
  ""isHotDeal"": true,
  ""stockQuantity"": 45,
  ""categoryId"": ""77bc261c-02ef-499a-8f97-f085ab3f5592"",
  ""prices"": [
    {
      ""userRole"": ""NormalUser"",
      ""price"": 1299.99,
      ""discountedPrice"": 1199.99,
      ""discountPercentage"": 7.69
    }
  ]
}")
                    };
                }
            }
        }

        // Special handling for UpdateProductWithImage endpoint
        if (context.MethodInfo.Name == "UpdateProductWithImage")
        {
            // Find the productData parameter in the request body
            if (operation.RequestBody?.Content?.ContainsKey("multipart/form-data") == true)
            {
                var schema = operation.RequestBody.Content["multipart/form-data"].Schema;
                if (schema?.Properties?.ContainsKey("productData") == true)
                {
                    // Replace the productData string schema with UpdateProductDto schema
                    var productDataSchema = context.SchemaGenerator.GenerateSchema(typeof(UpdateProductDto), context.SchemaRepository);
                    schema.Properties["productData"] = new OpenApiSchema
                    {
                        Type = "string",
                        Description = "JSON string containing product update data",
                        Example = new Microsoft.OpenApi.Any.OpenApiString(
                            @"{
  ""name"": ""Updated Product Name"",
  ""description"": ""Updated description"",
  ""shortDescription"": ""Updated short description"",
  ""isActive"": true,
  ""isHotDeal"": true,
  ""stockQuantity"": 100,
  ""categoryId"": ""77bc261c-02ef-499a-8f97-f085ab3f5592"",
  ""brandId"": ""88bc261c-02ef-499a-8f97-f085ab3f5593"",
  ""prices"": [
    {
      ""userRole"": ""NormalUser"",
      ""price"": 199.99,
      ""discountedPrice"": 179.99,
      ""discountPercentage"": 10.0
    }
  ]
}")
                    };
                }
            }
        }

        // Special handling for UpdateProductWithFiles endpoint
        if (context.MethodInfo.Name == "UpdateProductWithFiles")
        {
            // Find the productData parameter in the request body
            if (operation.RequestBody?.Content?.ContainsKey("multipart/form-data") == true)
            {
                var schema = operation.RequestBody.Content["multipart/form-data"].Schema;
                if (schema?.Properties?.ContainsKey("productData") == true)
                {
                    // Replace the productData string schema with UpdateProductDto schema
                    var productDataSchema = context.SchemaGenerator.GenerateSchema(typeof(UpdateProductDto), context.SchemaRepository);
                    schema.Properties["productData"] = new OpenApiSchema
                    {
                        Type = "string",
                        Description = "JSON string containing product update data with SKU, detail image, and PDF support",
                        Example = new Microsoft.OpenApi.Any.OpenApiString(
                            @"{
  ""name"": ""Updated Product with Files"",
  ""description"": ""Product with SKU, detail image, and PDF support"",
  ""shortDescription"": ""Updated product"",
  ""sku"": ""TEST-SKU-001"",
  ""isActive"": true,
  ""isHotDeal"": false,
  ""stockQuantity"": 100,
  ""categoryId"": ""77bc261c-02ef-499a-8f97-f085ab3f5592"",
  ""brandId"": ""88bc261c-02ef-499a-8f97-f085ab3f5593"",
  ""prices"": [
    {
      ""userRole"": ""NormalUser"",
      ""price"": 99.99,
      ""discountedPrice"": 89.99,
      ""discountPercentage"": 10.0
    }
  ]
}")
                    };
                }
            }
        }
    }
}
