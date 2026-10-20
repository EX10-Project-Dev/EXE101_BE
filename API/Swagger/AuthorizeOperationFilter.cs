using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Swagger;

public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.GetCustomAttributes(inherit: true).OfType<IAllowAnonymous>().Any()
            || context.MethodInfo.DeclaringType?.GetCustomAttributes(inherit: true).OfType<IAllowAnonymous>().Any() == true)
            return;
        var requiresAuthorization = context.MethodInfo.GetCustomAttributes(inherit: true).OfType<IAuthorizeData>().Any()
            || context.MethodInfo.DeclaringType?.GetCustomAttributes(inherit: true).OfType<IAuthorizeData>().Any() == true;

        if (!requiresAuthorization) return;

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
        });
    }
}
