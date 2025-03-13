using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;

// Top-level statements
var builder = WebApplication.CreateBuilder(args);

// Add services for logging.
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});

var app = builder.Build();

// Register the ExceptionHandlingMiddleware
app.UseExceptionHandlingMiddleware();

// Add the TokenMiddleware
app.UseTokenMiddleware();

// Add HTTP logging middleware.
app.UseHttpLogging();

// In-memory list to store users
var users = new List<User>();

// GET: Fetch all users
// app.MapGet("/api/users", async context =>
// {
//     await context.Response.WriteAsJsonAsync(users);
// });
// Enhanced with pagination.
app.MapGet("/api/users", async (HttpContext context) =>
{
    Console.WriteLine("Get ALL users:");
    Console.WriteLine($"Request Path: {context.Request.Path}"); // Log the request path.
    int page = int.TryParse(context.Request.Query["page"], out var p) ? p : 1;
    int pageSize = int.TryParse(context.Request.Query["pageSize"], out var ps) ? ps : 10;

    var pagedUsers = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    await context.Response.WriteAsJsonAsync(pagedUsers);
    Console.WriteLine($"Response Status Code: {context.Response.StatusCode}"); // Log the response status code.
});


// GET: Fetch a user by username
app.MapGet("/api/users/{username}", async (HttpContext context, string username) =>
{
    Console.WriteLine("Get a user by name:");
    var user = users.FirstOrDefault(u => u.Username == username);
    if (user == null)
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("User not found");
        return;
    }
    await context.Response.WriteAsJsonAsync(user);
    Console.WriteLine($"Response Status Code: {context.Response.StatusCode}"); // Log the response status code.
});

// POST: Create a new user.
// Enhanced with error handling.
app.MapPost("/api/users", async (HttpContext context) =>
{
    Console.WriteLine("Create user:");
    Console.WriteLine($"Request Path: {context.Request.Path}"); // Log the request path.
    try
    {
        var newUser = await JsonSerializer.DeserializeAsync<User>(context.Request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        if (newUser == null || string.IsNullOrWhiteSpace(newUser.Username) || newUser.UserAge <= 0 || users.Any(u => u.Username == newUser.Username))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid input or username already exists");
            return;
        }

        users.Add(newUser);
        context.Response.StatusCode = 201;
        await context.Response.WriteAsJsonAsync(newUser);
    }
    catch (JsonException)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Invalid JSON format");
    }
    Console.WriteLine($"Response Status Code: {context.Response.StatusCode}"); // Log the response status code.
});

// PUT: Update an existing user's age.
// Enhanced with error handling.
app.MapPut("/api/users/{username}", async (HttpContext context, string username) =>
{
    Console.WriteLine("Update user:");
    Console.WriteLine($"Request Path: {context.Request.Path}"); // Log the request path.
    try
    {
        var user = users.FirstOrDefault(u => u.Username == username);
        if (user == null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("User not found");
            return;
        }

        var updatedUser = await JsonSerializer.DeserializeAsync<User>(context.Request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (updatedUser == null || updatedUser.UserAge <= 0)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid input");
            return;
        }

        user.UserAge = updatedUser.UserAge;
        context.Response.StatusCode = 204;
    }
    catch (JsonException)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Invalid JSON format");
    }
     Console.WriteLine($"Response Status Code: {context.Response.StatusCode}"); // Log the response status code.
});

// DELETE: Remove a user by username.
// Enhanced with error handling.
app.MapDelete("/api/users/{username}", async (HttpContext context, string username) =>
{
    Console.WriteLine("Delete user:");
    Console.WriteLine($"Request Path: {context.Request.Path}"); // Log the request path.
    try
    {
        // Attempt to find the user by username
        var user = users.FirstOrDefault(u => u.Username == username);
        if (user == null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("User not found");
            return;
        }

        // Remove the user from the list
        users.Remove(user);
        context.Response.StatusCode = 204;
    }
    catch (Exception ex)
    {
        // Log the exception if necessary (not included here but can be added in a real-world scenario)
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync($"An unexpected error occurred: {ex.Message}");
    }
     Console.WriteLine($"Response Status Code: {context.Response.StatusCode}"); // Log the response status code.
});

// Expose an endpoint to generate and return the token (for testing only)
app.MapGet("/generate-token", async (HttpContext context) =>
{
    // Define the hard-coded secret key
    var secretKey = "UserManagementApiYourSuperSecretKey123!";

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    // Define token details
    var token = new JwtSecurityToken(
        issuer: "TestIssuer",
        audience: "TestAudience",
        claims: new[] { new Claim("role", "TestUser") },
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: creds);

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    // Return the generated token
    await context.Response.WriteAsync(tokenString);
});

// Run the application
app.Run();

// Define the User class (BEFORE top-level statements)
class User
{
    public string Username { get; set; }
    public int UserAge { get; set; }
}