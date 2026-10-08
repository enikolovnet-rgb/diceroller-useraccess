using DiceRoller.UserAccess.Application.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DiceRoller.UserAccess.Api.ModelBinding;

/// <summary>Binds a multipart file to <see cref="FileUpload"/> so the Application layer never sees <c>IFormFile</c>.</summary>
public sealed class FileUploadModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var request = bindingContext.HttpContext.Request;
        var file = request.HasFormContentType
            ? request.Form.Files.GetFile(bindingContext.ModelName) ?? request.Form.Files.GetFile(bindingContext.FieldName)
            : null;

        // A missing file binds to null and is reported by the validator, not by model binding.
        bindingContext.Result = ModelBindingResult.Success(file is null
            ? null
            : new FileUpload(file.OpenReadStream(), file.FileName, file.ContentType, file.Length));

        return Task.CompletedTask;
    }
}
