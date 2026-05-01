using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ResX.Common.Filters.Swagger;

/// <summary>
/// Adds every non-nullable property to the schema's `required` list.
/// SupportNonNullableReferenceTypes() only flips `nullable: false`; this filter
/// is what makes the OpenAPI schema actually flag the field as required.
/// </summary>
internal sealed class RequireNonNullablePropertiesSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null || schema.Properties.Count == 0)
            return;

        foreach (var (name, property) in schema.Properties)
        {
            if (!property.Nullable)
            {
                schema.Required.Add(name);
            }
        }
    }
}
