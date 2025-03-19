using InternetVotingApplication;
using Microsoft.AspNetCore.Builder;

// Create the builder
var builder = WebApplication.CreateBuilder(args);

// Initialize Startup class
var startup = new Startup(builder.Configuration);

// Configure services
startup.ConfigureServices(builder.Services);

// Build the app
var app = builder.Build();

// Configure the app
startup.Configure(app, app.Environment);

// Run the app
app.Run();
