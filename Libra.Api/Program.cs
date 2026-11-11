var builder = WebApplication.CreateBuilder(args);
builder.AddErrorHandling();
builder.Services.AddApiServices();
builder.Services.AddInfrastructure(
    builder.Environment.ContentRootPath);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var initializer = scope.ServiceProvider
        .GetRequiredService<DatabaseInitializer>();

    await initializer.InitializeAsync();
}

app.UseErrorHandling();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapEndpoints();

app.Run();