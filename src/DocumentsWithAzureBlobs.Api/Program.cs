using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("containers", async (IConfiguration configuration) =>
{
    var cnnString = configuration.GetConnectionString("AzureBlobs");
    var blobClient = new BlobServiceClient(cnnString);

    var resultSegment = blobClient.GetBlobContainersAsync(BlobContainerTraits.Metadata, default).AsPages(default, 10);

    var result = new List<string>();

    await foreach (Azure.Page<BlobContainerItem> containerPage in resultSegment)
    {
        foreach (var item in containerPage.Values)
        {
            result.Add(item.Name);
        }
    }

    return result;
}).WithOpenApi();

app.MapGet("containers/{container}/files", async (string container, IConfiguration configuration) => {
    var cnnString = configuration.GetConnectionString("AzureBlobs");
    var blobServiceClient = new BlobServiceClient(cnnString);
    var blobContainer = blobServiceClient.GetBlobContainerClient(container);

    var blobs = blobContainer.GetBlobsAsync();

    var result = new List<string>();

    await foreach (BlobItem blobItem in blobs)
    {
        result.Add(blobItem.Name);
    }

    return result;
});

app.MapGet("file/{name}", async (string name, IConfiguration configuration) => {
    var cnnString = configuration.GetConnectionString("AzureBlobs");
    var blobServiceClient = new BlobServiceClient(cnnString);
    var blobContainer = blobServiceClient.GetBlobContainerClient("hml");
    var blobClient = blobContainer.GetBlobClient(name);

    BlobDownloadResult result = await blobClient.DownloadContentAsync();

    var content = Convert.ToBase64String(result.Content);

    return content;
}).WithOpenApi();

app.MapPost("file", async (string filePath, IConfiguration configuration) => {
    var cnnString = configuration.GetConnectionString("AzureBlobs");
    var blobClient = new BlobServiceClient(cnnString);

    var fileStream = File.OpenRead(filePath);

    var blobContainer = blobClient.GetBlobContainerClient("hml");

    await blobContainer.UploadBlobAsync(Guid.NewGuid().ToString(), fileStream);

    return Results.Ok();
})
.WithOpenApi();

app.MapDelete("file/{name}", async (string name, IConfiguration configuration) => {
    var cnnString = configuration.GetConnectionString("AzureBlobs");
    var blobServiceClient = new BlobServiceClient(cnnString);
    var blobContainer = blobServiceClient.GetBlobContainerClient("hml");
    var blobClient = blobContainer.GetBlobClient(name);

    await blobClient.DeleteAsync();

    return Results.NoContent();
})
.WithOpenApi();



app.Run();
