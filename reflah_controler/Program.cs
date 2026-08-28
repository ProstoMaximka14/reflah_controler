using Microsoft.Extensions.FileProviders;
using reflah_controler.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorOptions(o => o.ViewLocationFormats.Add("/Views/Home/{0}.cshtml"));

builder.Services.AddSignalR();

var app = builder.Build();

// Разрешаем доступ к общей папке
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(@"C:\fotos"),
    RequestPath = "/shared-fotos"
});

app.UseStaticFiles(); // Для wwwroot

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapPost("/api/db-notify", () =>
{
    Console.WriteLine($"🔔 [{DateTime.Now}] ПОЛУЧЕН СИГНАЛ: База данных изменена!");
    return Results.Ok();
});

app.MapHub<DatabaseHub>("/databaseHub");

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
