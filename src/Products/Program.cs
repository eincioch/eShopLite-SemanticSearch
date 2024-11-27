using Microsoft.EntityFrameworkCore;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using Products.Endpoints;
using Products.Memory;
using Products.Models;

var builder = WebApplication.CreateBuilder(args);

// Disable Globalization Invariant Mode
Environment.SetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "false");

// add aspire service defaults
builder.AddServiceDefaults();

builder.Services.AddProblemDetails();

// Add DbContext service
builder.AddSqlServerDbContext<Context>("sqldb");

// Add Azure OpenAI client
var azureOpenAiClientName = builder.Environment.IsDevelopment() ? "azureOpenAIDev" : "azureOpenAI";
builder.AddAzureOpenAIClient(azureOpenAiClientName);

// get azure openai client and create Chat client from aspire hosting configuration
builder.Services.AddSingleton<ChatClient>(serviceProvider =>
{
    var config = serviceProvider.GetService<IConfiguration>()!;
    if (string.IsNullOrEmpty(config["AI_ChatDeploymentName"]))
    {
        config["AI_ChatDeploymentName"] = "chat";
    }

    var logger = serviceProvider.GetService<ILogger<Program>>()!;
    logger.LogInformation($"Chat client configuration, modelId: {config["AI_ChatDeploymentName"]}");

    OpenAIClient client = serviceProvider.GetRequiredService<OpenAIClient>();
    var chatClient = client.GetChatClient(config["AI_ChatDeploymentName"]);
    return chatClient;
});

// get azure openai client and create embedding client from aspire hosting configuration
builder.Services.AddSingleton<EmbeddingClient>(serviceProvider =>
{
    var config = serviceProvider.GetService<IConfiguration>()!;
    if (string.IsNullOrEmpty(config["AI_EmbeddingsDeploymentName"]))
    {
        config["AI_EmbeddingsDeploymentName"] = "embeddings";
    }

    var logger = serviceProvider.GetService<ILogger<Program>>()!;
    logger.LogInformation($"Embeddings client configuration, modelId: {config["AI_EmbeddingsDeploymentName"]}");

    OpenAIClient client = serviceProvider.GetRequiredService<OpenAIClient>();
    var embeddingsClient = client.GetEmbeddingClient(config["AI_EmbeddingsDeploymentName"]);
    return embeddingsClient;
});

builder.Services.AddSingleton<IConfiguration>(sp =>
{
    return builder.Configuration;
});

// add memory context
builder.Services.AddSingleton(sp =>
{
    return new MemoryContext();
});

// Add services to the container.
var app = builder.Build();

// aspire map default endpoints
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.MapProductEndpoints();

app.UseStaticFiles();

// log Azure OpenAI resources
app.Logger.LogInformation($"Azure OpenAI resources\n >> OpenAI Client Name: {azureOpenAiClientName}");

// manage db
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<Context>();
    try
    {
        app.Logger.LogInformation("Ensure database created");
        context.Database.EnsureCreated();
    }
    catch (Exception exc)
    {
        app.Logger.LogError(exc, "Error creating database");
    }
    DbInitializer.Initialize(context);
}

// init semantic memory
app.InitSemanticMemory();

app.Run();