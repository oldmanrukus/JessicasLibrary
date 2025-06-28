using JessicasLibrary.Data;
using JessicasLibrary.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1) EF Core + SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// 2) Developer exception filter (shows EF errors in dev)
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 3) Identity + Roles backed by EF Core
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 4) Razor Pages
builder.Services.AddRazorPages();

// 5) Firebase & Speech services
builder.Services.Configure<FirebaseOptions>(
    builder.Configuration.GetSection("Firebase")
);
builder.Services.AddHttpClient();
builder.Services.AddScoped<FirebaseService>();
builder.Services.AddScoped<AzureSpeechService>();

var app = builder.Build();

// 6) On startup: apply migrations, seed default admin, promote your user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();

    // Applies any pending migrations (creates DB + tables on first run)
    db.Database.Migrate();

    // Seed the "oldmanrukus" admin account/role
    await SeedData.InitializeAsync(services);

    // Promote your account (rukzero@gmail.com) to Admin if it exists
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var user = await userManager.FindByEmailAsync("rukzero@gmail.com");
    if (user != null && !await userManager.IsInRoleAsync(user, "Admin"))
    {
        await userManager.AddToRoleAsync(user, "Admin");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
