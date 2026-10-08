namespace DiceRoller.UserAccess.Application.Common;

/// <summary>An uploaded file. <see cref="Content"/> must be seekable so its header can be inspected before it is stored.</summary>
public sealed record FileUpload(Stream Content, string FileName, string ContentType, long Length);
