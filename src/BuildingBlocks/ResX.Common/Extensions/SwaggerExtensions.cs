using Microsoft.Extensions.DependencyInjection;
using ResX.Common.Filters.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ResX.Common.Extensions;

public static class SwaggerExtensions
{
    /// <summary>
    /// Marks non-nullable reference properties as required in OpenAPI schemas.
    /// Relies on &lt;Nullable&gt;enable&lt;/Nullable&gt; in the DTO project — without NRT,
    /// every reference type is treated as nullable and nothing becomes required.
    /// </summary>
    public static SwaggerGenOptions ApplyResXDefaults(this SwaggerGenOptions options)
    {
        options.SupportNonNullableReferenceTypes();
        options.UseAllOfToExtendReferenceSchemas();
        options.SchemaFilter<RequireNonNullablePropertiesSchemaFilter>();
        return options;
    }
}