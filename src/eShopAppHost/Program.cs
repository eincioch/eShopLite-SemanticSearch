using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var chatDeploymentName = "chat";
var embeddingsDeploymentName = "embeddings";
var azureOpenAI = builder.AddAzureOpenAI("azureOpenAI")
    .AddDeployment(new AzureOpenAIDeployment(chatDeploymentName,
    "gpt-4o-mini",
    "2024-07-18",
    "GlobalStandard",
    10))
    .AddDeployment(new AzureOpenAIDeployment(embeddingsDeploymentName,
    "text-embedding-ada-002",
    "2"));

var sqldb = builder.AddSqlServer("sql")
    .WithDataVolume()
    .AddDatabase("sqldb");

var products = builder.AddProject<Projects.Products>("products")
    .WithReference(sqldb);

// check if working in dev environment
// remove the if, if you want to use the references also in development
if (!builder.Environment.IsDevelopment())
{
    products.WithReference(azureOpenAI);
    products.WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);
    products.WithEnvironment("AI_EmbeddingsDeploymentName", embeddingsDeploymentName);
}

var store = builder.AddProject<Projects.Store>("store")
    .WithReference(products)
    .WithExternalHttpEndpoints();

builder.Build().Run();
