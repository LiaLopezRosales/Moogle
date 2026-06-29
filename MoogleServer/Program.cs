using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using MoogleEngine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddRateLimiter(options =>
{
  options.AddFixedWindowLimiter("Api", opt =>
  {
    opt.PermitLimit = 100;
    opt.Window = TimeSpan.FromMinutes(1);
    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    opt.QueueLimit = 5;
  });

  options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Moogle! starting — Environment: {Env}", app.Environment.EnvironmentName);

if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error");
  app.UseHsts();
}
else
{
  app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.MapGet("/api/search", (string q) =>
{
  var result = MoogleEngine.Moogle.Query(q);
  return Results.Ok(new
  {
    items = result.Items().Select(i => new
    {
      title = i.Title,
      snippet = i.Snippet,
      score = i.Score
    }),
    suggestion = result.Suggestion,
    count = result.Count
  });
}).RequireRateLimiting("Api");

var dataLogger = app.Services.GetRequiredService<ILogger<DataBase>>();
MoogleEngine.Moogle.data = new DataBase(logger: dataLogger);
logger.LogInformation("Moogle! ready");
app.Run();
