using DiceRoller.UserAccess.Application.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace DiceRoller.UserAccess.Api.ModelBinding;

public sealed class FileUploadModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Metadata.ModelType == typeof(FileUpload)
            ? new BinderTypeModelBinder(typeof(FileUploadModelBinder))
            : null;
    }
}
