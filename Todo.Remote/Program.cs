using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Todo.Remote.Domain.Entities;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddDbContext<IdentityContext>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();
builder.Services.AddHttpContextAccessor();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<IdentityContext>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(transformBuilderContext =>  // Add transforms inline
    {
        // For each route+cluster pair decide if we want to add transforms, and if so, which?
        // This logic is re-run each time a route is rebuilt.

        // Only do this for routes that require auth.
        if (string.Equals("Default", transformBuilderContext.Route.AuthorizationPolicy))
        {
            transformBuilderContext.AddRequestTransform(transformContext =>
            {
                var userName = transformContext?.HttpContext?.User?.Identity?.Name;

                // Reject invalid requests
                if (string.IsNullOrEmpty(userName))
                {
                    var response = transformContext?.HttpContext?.Response;

                    if (response is not null)
                        response.StatusCode = 401;

                    return ValueTask.FromCanceled(new CancellationToken());
                }

                transformContext?.ProxyRequest?.Headers?.Add("x-usr", userName);

                return ValueTask.CompletedTask;
            });
        }
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapIdentityApi<IdentityUser>();
app.UseRouting();
app.MapControllers().WithOpenApi();
app.MapHealthChecks("/health-check");

app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
