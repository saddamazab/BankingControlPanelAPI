using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure SQL Server with a file-based approach
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//swagger
builder.Services.AddControllers();
 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllers();

var app = builder.Build();

// Ensure DataDirectory is set
AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(Directory.GetCurrentDirectory(), "Data"));

if (app.Environment.IsDevelopment())
{
    //app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Role creation logic (if needed)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    //await CreateRoles(roleManager, userManager);
}

app.Run();

// Role creation method
//async Task CreateRoles(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
//{
//    string[] roleNames = { "Admin", "User" };
//    IdentityResult roleResult;

//    foreach (var roleName in roleNames)
//    {
//        var roleExist = await roleManager.RoleExistsAsync(roleName);
//        if (!roleExist)
//        {
//            roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
//        }
//    }

//    var powerUser = new IdentityUser
//    {
//        UserName = "admin@domain.com",
//        Email = "admin@domain.com",
//    };

//    string userPassword = "Admin@2030";
//    var user = await userManager.FindByEmailAsync("admin@domain.com");

//    if (user == null)
//    {
//        var createPowerUser = await userManager.CreateAsync(powerUser, userPassword);
//        if (createPowerUser.Succeeded)
//        {
//            await userManager.AddToRoleAsync(powerUser, "Admin");
//        }
//    }
//  }
