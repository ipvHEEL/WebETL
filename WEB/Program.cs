//SSISDashboard/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(); 
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllers(); 
app.MapRazorPages();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // покажет ошибку в браузере
}
app.Run();