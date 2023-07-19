using Bongo.Data;
using Bongo.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
builder.Services.AddTransient<IMailService, MailService>();
/*builder.Services.AddScoped<IHttpContextAccessor , HttpContextAccessor>(); 
*/
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

builder.Services.AddDbContext<AppIdentityDbContext>(options => 
    options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>( options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 5;
    options.User.RequireUniqueEmail = true; 
}).AddEntityFrameworkStores<AppIdentityDbContext>().AddTokenProvider<DataProtectorTokenProvider<IdentityUser>>(TokenOptions.DefaultProvider); 

var app = builder.Build();


app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();    
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=LandingPage}/{action=Index}/{id?}/{string?}");

SeedData.EnsurePopulated(app);
SeedData.EnsureIdentityPopulated(app);
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(app.Configuration.GetValue<string>("SyncfusionKey:Key"));

app.Run();
