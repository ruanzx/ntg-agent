using MyTestMCPServer;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<MonkeyService>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();
var app = builder.Build();

app.MapMcp();
app.MapDefaultEndpoints();

app.Run();
