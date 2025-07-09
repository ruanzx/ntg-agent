using Microsoft.SemanticKernel;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Client;
using NTG.Agent.Orchestrator.Agents;
using NTG.Agent.Orchestrator.Plugins;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

await using IMcpClient mcpClient = await McpClientFactory.CreateAsync(
    new SseClientTransport(new()
    {
        Name = "MyTestMCP",
        Endpoint = new Uri("http://localhost:5051")
    })
);

var tools = await mcpClient.ListToolsAsync();
foreach (var tool in tools)
{
    Console.WriteLine($"Tool: {tool.Name} - {tool.Description}");
}

builder.Services.AddSingleton<Kernel>(serviceBuilder => { 
    var config = serviceBuilder.GetRequiredService<IConfiguration>();
    var kernelBuilder = Kernel.CreateBuilder();

    // Add Azure OpenAI
    if (config["Azure:OpenAI:Endpoint"] != null && config["Azure:OpenAI:ApiKey"] != null && config["Azure:OpenAI:DeploymentName"] != null)
    {
        kernelBuilder.AddAzureOpenAIChatCompletion(
            endpoint: config["Azure:OpenAI:Endpoint"]!,
            apiKey: config["Azure:OpenAI:ApiKey"]!,
            deploymentName: config["Azure:OpenAI:DeploymentName"]!,
            serviceId: "aoai");
    }

    // Add GitHub Models
    if (config["GitHub:Models:GitHubToken"] != null && config["GitHub:Models:Endpoint"] != null && config["GitHub:Models:ModelId"] != null)
    {
        var credentials = new ApiKeyCredential(config["GitHub:Models:GitHubToken"]!);
        var options = new OpenAIClientOptions { Endpoint = new Uri(config["GitHub:Models:Endpoint"]!) };
        var client = new OpenAIClient(credentials, options);
        kernelBuilder.AddOpenAIChatCompletion(
            openAIClient: client,
            modelId: config["GitHub:Models:ModelId"]!,
            serviceId: "github");
    }
    
    var kernel = kernelBuilder.Build();

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    kernel.Plugins.AddFromFunctions("MyTestMCP", tools.Select(x => x.AsKernelFunction()));
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    kernel.Plugins.Add(KernelPluginFactory.CreateFromType<DateTimePlugin>());

    return kernel;
});

builder.Services.AddScoped<IAgentService, AgentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
