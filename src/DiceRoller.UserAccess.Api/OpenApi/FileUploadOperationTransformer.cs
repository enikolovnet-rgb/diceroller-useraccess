using DiceRoller.UserAccess.Application.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace DiceRoller.UserAccess.Api.OpenApi;

/// <summary>
/// Describes <see cref="FileUpload"/> form fields as binary files in a <c>multipart/form-data</c> body, the way
/// <c>IFormFile</c> is described. Without it the generator sees an ordinary object and flattens its stream properties.
/// </summary>
public sealed class FileUploadOperationTransformer : IOpenApiOperationTransformer
{
    private const string MultipartFormData = "multipart/form-data";

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var fileFields = context.Description.ActionDescriptor.Parameters
            .Where(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Form)
            .SelectMany(parameter => parameter.ParameterType.GetProperties())
            .Where(property => property.PropertyType == typeof(FileUpload))
            .Select(property => property.Name)
            .ToList();

        if (fileFields.Count == 0
            || operation.RequestBody is not OpenApiRequestBody { Content.Count: > 0 } requestBody
            || requestBody.Content.Values.First().Schema is not OpenApiSchema { Properties: not null } schema)
        {
            return Task.CompletedTask;
        }

        foreach (var field in fileFields)
        {
            foreach (var flattened in schema.Properties.Keys.Where(key => key.StartsWith($"{field}.", StringComparison.Ordinal)).ToList())
            {
                schema.Properties.Remove(flattened);
            }

            schema.Properties[field] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" };
        }

        requestBody.Content = new Dictionary<string, OpenApiMediaType> { [MultipartFormData] = new() { Schema = schema } };

        return Task.CompletedTask;
    }
}
