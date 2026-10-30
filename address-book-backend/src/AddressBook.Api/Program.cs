using AddressBook.Api.Middleware;
using AddressBook.Core.Interfaces;
using AddressBook.Core.Services;
using AddressBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("AddressBook");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings:AddressBook before starting the API.");
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddDbContext<AddressBookDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<IAddressBookStore, AddressBookStore>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<ContactService>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.MapControllers();
app.Run();

public partial class Program;
