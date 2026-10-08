var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.Run();

/// <summary>Entry point; public so integration tests can use it with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
