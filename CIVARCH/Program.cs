using CIVARCH.Pages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();

var config = new ConfigurationBuilder()
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Local.json", optional: true)   // lokální hesla – nezahrnuto do gitu
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

double cookieTimeout;
if (!Double.TryParse(config["LdapSettings:CookieTimeout"], out cookieTimeout))
{
    throw new ArgumentException("CookieTimeout was either not a number or null");
}

// Adds authentication and configurates cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(options =>
{
    options.LoginPath = "/Login";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(cookieTimeout);
    options.SlidingExpiration = true;
    options.Cookie.MaxAge = TimeSpan.FromMinutes(cookieTimeout);
});

// Builds the builder
// Make sure any builder config is done before build
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

// Makes sure asp.net authenticates user
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
