using AdminBlazor.Test.Components;
using FreeSql;
using Newtonsoft.Json;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAdminBlazor(new AdminBlazorOptions
{
    Assemblies = [typeof(Program).Assembly],
    FreeSqlBuilder = a => a
        .UseConnectionString(DataType.Sqlite, @"Data Source=freedb.db")
        .UseMonitorCommand(cmd => System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {cmd.CommandText}\r\n"))//����SQL���
        .UseAutoSyncStructure(true) //�Զ�ͬ��ʵ��ṹ�����ݿ⣬FreeSql����ɨ����򼯣�ֻ��CRUDʱ�Ż����ɱ�
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
