using AppBase;
using AppBase.Redis;
using BlazorApp1.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.Reflection;
using static AppBase.Redis.RedisConnector;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddHttpClient(); // 이걸 추가하면 HttpClient 주입이 가능해져



#region  Redis Database
builder.Services.AddSingleton<RedisDatabase>();
builder.Services.AddSingleton<RedisServer>();
#endregion

var app = builder.Build();

await ProgramStart();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

async Task ProgramStart()
{
    ConfigurationHelper.Provider = app.Services;
    ConfigurationHelper.IsDevelopment = app.Environment.IsDevelopment();
    /*
    var redisDatabase = (RedisDatabase?)app.Services.GetService(typeof(RedisDatabase));
    var redisDb = redisDatabase!.GetDatabase();

    //저장
    string redisKey = "ruisession";
    await redisDb.StringSetAsync(redisKey, "abcdefg");

    //레디스Key Limit설정
    var keyLimit = DateTime.UtcNow.AddDays(300);

    //key리밋
    await redisDb.KeyExpireAsync(redisKey, keyLimit);


    var userSession = await redisDb.StringGetAsync<string>(redisKey);
    */
}