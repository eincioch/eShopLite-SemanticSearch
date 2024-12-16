var builder = DistributedApplication.CreateBuilder(args);

var appInsights = builder.AddAzureApplicationInsights("appInsights");

var chatDeploymentName = "gpt-4o-mini";
var embeddingsDeploymentName = "text-embedding-ada-002";
var aoai = builder.AddAzureOpenAI("openai")
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
    .WithReference(sqldb)
    .WaitFor(sqldb)
    .WithReference(appInsights)
    .WithReference(aoai)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName)
    .WithEnvironment("AI_embeddingsDeploymentName", embeddingsDeploymentName);

var store = builder.AddProject<Projects.Store>("store")
    .WithReference(products)
    .WaitFor(products)
    .WithReference(appInsights)
    .WithExternalHttpEndpoints();

builder.Build().Run();
