using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VertexLearn.Data;
using VertexLearn.Data.Repositories;
using VertexLearn.Models;

var builder = WebApplication.CreateBuilder(args);

// MVC + a global anti-forgery check on every POST form
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    // Display-only properties (e.g. CourseTitle) should not be treated as [Required]
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// Cookie authentication with role-based authorization (Student / Admin)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = "VertexLearn.Auth";
    });
builder.Services.AddAuthorization();

// Data access layer (ADO.NET + SQL Server)
builder.Services.AddSingleton<Db>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<CourseRepository>();
builder.Services.AddScoped<ResourceRepository>();
builder.Services.AddScoped<EnrollmentRepository>();
builder.Services.AddScoped<QuizRepository>();
builder.Services.AddScoped<DiscussionRepository>();
builder.Services.AddScoped<ContactRepository>();

var app = builder.Build();

// Create the database, tables and sample data on first run
if (app.Configuration.GetValue("Database:CreateOnStartup", true))
{
    await DbInitializer.InitializeAsync(app.Configuration, app.Environment, app.Logger);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
