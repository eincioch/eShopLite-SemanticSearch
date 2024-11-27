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
    var chatDeploymentName = "gpt-4o-mini";
    var logger = serviceProvider.GetService<ILogger<Program>>()!;
    logger.LogInformation($"Chat client configuration, modelId: {chatDeploymentName}");
    ChatClient chatClient = null;
    try
    {
        OpenAIClient client = serviceProvider.GetRequiredService<OpenAIClient>();
        chatClient = client.GetChatClient(chatDeploymentName);

    }
    catch (Exception exc)
    {
        logger.LogError(exc, "Error creating embeddings client");
    }
    return chatClient;
});

// get azure openai client and create embedding client from aspire hosting configuration
builder.Services.AddSingleton<EmbeddingClient>(serviceProvider =>
{
    var embeddingsDeploymentName = "text-embedding-ada-002";
    var logger = serviceProvider.GetService<ILogger<Program>>()!;
    logger.LogInformation($"Embeddings client configuration, modelId: {embeddingsDeploymentName}");
    EmbeddingClient embeddingsClient = null;
    try
    {
        OpenAIClient client = serviceProvider.GetRequiredService<OpenAIClient>();
        embeddingsClient = client.GetEmbeddingClient(embeddingsDeploymentName);
    }
    catch (Exception exc)
    {
        logger.LogError(exc, "Error creating embeddings client");
    }
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