namespace DiceRoller.UserAccess.IntegrationTests.Infrastructure;

public static class TestPhotos
{
    /// <summary>A valid 1×1 PNG.</summary>
    public static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    public static readonly byte[] Gif = [.. "GIF89a"u8, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00];

    public static readonly byte[] Html = "<script>alert(1)</script>"u8.ToArray();
}
