using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MinecraftSkins.Api.Swagger;

public class IdempotencyHeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if the endpoint path contains "/api/purchases" and the method is POST
        // This targets the CreatePurchase endpoint specifically.
        if (context.ApiDescription.HttpMethod?.Equals("POST", StringComparison.OrdinalIgnoreCase) == true &&
            context.ApiDescription.RelativePath?.Contains("api/purchases", StringComparison.OrdinalIgnoreCase) == true)
        {
            operation.Parameters ??= new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Idempotency-Key",
                In = ParameterLocation.Header,
                Description = "Unique key to prevent duplicate purchases (UUID)",
                Required = true,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Format = "uuid"
                }
            });
        }
    }
}

