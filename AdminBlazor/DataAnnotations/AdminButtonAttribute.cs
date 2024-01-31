using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Rougamo.Context;

[AttributeUsage(AttributeTargets.Method)]
public class AdminButtonAttribute
    : Rougamo.MoAttribute
{
    public string Name { get; set; }

    public AdminButtonAttribute(string name)
    {
        Name = name;
    }

    public override void OnEntry(MethodContext context)
    {
        Console.WriteLine($"AdminButton -> {Name}");
        var targetType = context.Target.GetType();
        var service = targetType.GetPropertyOrFieldValue(context.Target, "ServiceProvider") as IServiceProvider;
        if (service == null)
        {
            context.Exception = new Exception($"{targetType.DisplayCsharp()} 未定义 IServiceProvider ServiceProvider {{ get; set; }}");
            return;
        }
        var login = service.GetService<AdminLoginInfo>();
        var JS = service.GetService<IJSRuntime>();

        Auth().ConfigureAwait(false).GetAwaiter().GetResult();

        async Task Auth()
        {
            if (Name == "AdminTable2_Refersh")
            {
                if (!await login.AuthPath(login.CurrentMenu?.PathLower))
                {
                    _ = JS.Error("没有访问权限."); //加 await 会卡死，貌似 JSRuntime 不能回传
                }
                return;
            }

            if (!await login.AuthButton(Name))
            {
                context.ReplaceReturnValue(this, context.RealReturnType.CreateInstanceGetDefaultValue());
                //await service.GetService<SwalService>().ShowModal(new SwalOption()
                //{
                //    Category = SwalCategory.Error,
                //    Title = "没有访问权限.",
                //    ShowClose = true,
                //});
                _ = JS.Error("没有访问权限."); //加 await 会卡死，貌似 JSRuntime 不能回传
                //var component = (context.Target as ComponentBase);
                //var invokeAsync = component.GetType().GetMethod("InvokeAsync", BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(Func<Task>) });
                //var invokeAsyncTask = invokeAsync.Invoke(context.Target, new object[] { async () => await JS.Error("没有访问权限11.") }) as Task;
                //await invokeAsyncTask;
            }
        }
    }
}