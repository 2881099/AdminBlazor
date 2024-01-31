using AdminBlazor.Template.Components;
using FreeSql;
using Newtonsoft.Json;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAdminBlazor(new AdminBlazorOptions
{
    Assemblies = [typeof(Program).Assembly],
    FreeSqlBuilder = a => a
        .UseConnectionString(DataType.Sqlite, @"Data Source=freedb.db")
        .UseMonitorCommand(cmd => System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {cmd.CommandText}\r\n"))//监听SQL语句
        .UseAutoSyncStructure(true) //自动同步实体结构到数据库，FreeSql不会扫描程序集，只有CRUD时才会生成表。
});
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddAdditionalAssemblies(typeof(AdminBlazorOptions).Assembly)
    .AddInteractiveServerRenderMode();

app.Run();
