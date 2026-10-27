using tickets_core.Infrastructure;
using tickets_core.src.Handlers;

DotNetEnv.Env.NoClobber().Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var runSeed = args.Contains("--seed", StringComparer.OrdinalIgnoreCase);
var builder = WebApplication.CreateBuilder(args.Where(arg =>
    !string.Equals(arg, "--seed", StringComparison.OrdinalIgnoreCase)).ToArray());

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers(options => options.Filters.Add<ApiResponseFilter>())
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
            new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
                ApiResponse<object>.Failure("La solicitud contiene errores de validación.",
                    context.HttpContext.TraceIdentifier,
                    context.ModelState.Where(entry => entry.Value?.Errors.Count > 0)
                        .ToDictionary(entry => entry.Key,
                            entry => entry.Value!.Errors.Select(error =>
                                string.IsNullOrEmpty(error.ErrorMessage)
                                    ? "El valor proporcionado no es válido."
                                    : error.ErrorMessage).ToArray())));
    });
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (runSeed)
{
    await tickets_core.Infrastructure.Persistence.Seeds.DatabaseSeed.RunAsync(app.Services, app.Configuration);
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
{
    await context.HttpContext.Response.WriteAsJsonAsync(
        ApiResponse<object>.Failure(
            Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(
                context.HttpContext.Response.StatusCode), context.HttpContext.TraceIdentifier));
});
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
