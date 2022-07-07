using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using InernetVotingApplication;
using InernetVotingApplication.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.KeyVault;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"),
    new DefaultAzureCredential());

// Add services to the container.
builder.Services.AddDbContext<InternetVotingContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString(builder.Configuration["Default"])));

var startup = new Startup(builder.Configuration);

startup.ConfigureServices(builder.Services);

var app = builder.Build();

startup.Configure(app, app.Environment);

app.Run();