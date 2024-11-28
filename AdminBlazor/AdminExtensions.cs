using BootstrapBlazor.Components;
using FreeScheduler;
using FreeSql;
using FreeSql.Aop;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Reflection;
using System.Web;
using Yitter.IdGenerator;

public class AdminBlazorOptions
{
    public ushort WorkId { get; set; } = 1;
    public string GeneartorKey { get; set; } = "10001";
    public string GeneartorServer { get; set; } = "http://47.245.82.251:31102";
    public Assembly[] Assemblies { get; set; }
    public Action<IServiceProvider, TaskInfo> SchedulerExecuting { get; set; }
    public Action<FreeSqlBuilder> FreeSqlBuilder { get; set; }

    internal static string Global_GeneartorKey;
    internal static string Global_GeneartorServer;
}

public static class AdminExtensions
{
    internal static AdminBlazorOptions Options { get; private set; }
    public static IServiceCollection AddAdminBlazor(this IServiceCollection services, AdminBlazorOptions options)
    {
        if (options == null) options = new AdminBlazorOptions();
        Options = options;
        AdminBlazorOptions.Global_GeneartorKey = options.GeneartorKey;
        AdminBlazorOptions.Global_GeneartorServer = options.GeneartorServer.TrimEnd('/');
        if (options.Assemblies == null) options.Assemblies = new[] { typeof(AdminExtensions).Assembly };
        else options.Assemblies = options.Assemblies.Concat(new[] { typeof(AdminExtensions).Assembly }).Distinct().ToArray();
        YitIdHelper.SetIdGenerator(new IdGeneratorOptions(options.WorkId) { WorkerIdBitLength = 6 });

        var cloud = new FreeSqlCloud();
        var mainEntities = new Dictionary<Type, bool>
        {
            [typeof(TenantEntity)] = true,
            [typeof(TenantDatabaseEntity)] = true,
            [typeof(TenantMenuEntity)] = true,
            [typeof(DictEntity)] = true,
        };
        cloud.EntitySteering = (_, e) =>
        {
            if (mainEntities.ContainsKey(e.EntityType))
            {
                e.DBKey = "main";
                return;
            }
        };
        cloud.Register("main", () =>
        {
            var mainBuilder = new FreeSqlBuilder()
                .UseNoneCommandParameter(true);
            if (options.FreeSqlBuilder != null) options.FreeSqlBuilder?.Invoke(mainBuilder);
            else mainBuilder
                .UseConnectionString(DataType.Sqlite, @"Data Source=master.db")
                .UseMonitorCommand(cmd => System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {cmd.CommandText}\r\n"))//监听SQL语句
                .UseAutoSyncStructure(true); //自动同步实体结构到数据库，FreeSql不会扫描程序集，只有CRUD时才会生成表
            var main = mainBuilder.Build();
            AdminContext.ConfigFreeSql(main);
            return main;
        });

        #region 初始化数据
        var fsql = cloud.Use("main");
        if (fsql.Select<MenuEntity>().Any() == false)
        {
            List<MenuEntity> getCudButtons(params MenuEntity[] btns) => new[]
            {
                new MenuEntity { Label = "添加", Path = "add", Sort = 10011, Type = MenuEntityType.按钮, IsSystem = true, },
                new MenuEntity { Label = "编辑", Path = "edit", Sort = 10012, Type = MenuEntityType.按钮, IsSystem = true, },
                new MenuEntity { Label = "删除", Path = "remove", Sort = 10013, Type = MenuEntityType.按钮, IsSystem = true, }
            }.Concat(btns ?? new MenuEntity[0]).ToList();
            var repo = fsql.GetAggregateRootRepository<MenuEntity>();
            repo.Insert(new[]
            {
                new MenuEntity
                {
                    Label = "系统管理",
                    Icon = "fa-code",
                    Path = "",
                    Sort = 1001,
                    Type = MenuEntityType.菜单,
                    IsSystem = true,
                    Childs = new List<MenuEntity>
                    {
                        new MenuEntity { Label = "用户", Path = "Admin/User", Sort = 10001, Type = MenuEntityType.菜单, IsSystem = true, Childs = getCudButtons(
                            new MenuEntity { Label = "分配角色", Path = "alloc_roles", Sort = 10014, Type = MenuEntityType.按钮, })
                        },
                        new MenuEntity { Label = "角色", Path = "Admin/Role", Sort = 10002, Type = MenuEntityType.菜单, IsSystem = true, Childs = getCudButtons(
                            new MenuEntity { Label = "分配用户", Path = "alloc_users", Sort = 10014, Type = MenuEntityType.按钮, IsSystem = true, },
                            new MenuEntity { Label = "分配菜单", Path = "alloc_menus", Sort = 10015, Type = MenuEntityType.按钮, IsSystem = true, })
                        },
                        new MenuEntity { Label = "菜单", Path = "Admin/Menu", Sort = 10003, Type = MenuEntityType.菜单, IsSystem = true, Childs = getCudButtons() },
                        new MenuEntity { Label = "定时任务", Path = "Admin/TaskScheduler", Sort = 10004, Type = MenuEntityType.菜单, IsSystem = true, Childs = new List<MenuEntity>
                            {
                                new MenuEntity { Label = "添加", Path = "add", Sort = 10011, Type = MenuEntityType.按钮, IsSystem = true, },
                                new MenuEntity { Label = "删除", Path = "remove", Sort = 10013, Type = MenuEntityType.按钮, IsSystem = true, },
                                new MenuEntity { Label = "暂停", Path = "pause", Sort = 10014, Type = MenuEntityType.按钮, IsSystem = true, },
                                new MenuEntity { Label = "恢复", Path = "resume", Sort = 10015, Type = MenuEntityType.按钮, IsSystem = true, },
                                new MenuEntity { Label = "立即触发", Path = "runnow", Sort = 10016, Type = MenuEntityType.按钮, IsSystem = true, },
                                new MenuEntity { Label = "查看日志", Path = "tasklog", Sort = 10017, Type = MenuEntityType.按钮, IsSystem = true, },
                                new MenuEntity { Label = "集群日志", Path = "clusterlog", Sort = 10018, Type = MenuEntityType.按钮, IsSystem = true, },
                            }
                        },
                        new MenuEntity { Label = "数据字典", Path = "Admin/Dict", Sort = 10005, Type = MenuEntityType.菜单, IsSystem = true, Childs = getCudButtons() },
                        new MenuEntity { Label = "租户", Path = "Admin/Tenant", Sort = 10006, Type = MenuEntityType.菜单, IsSystem = true, Childs = getCudButtons() },
                        new MenuEntity { Label = "数据库", Path = "Admin/TenantDatabase", Sort = 10007, Type = MenuEntityType.菜单, IsSystem = true, Childs = getCudButtons() },
                    }
                },
            });
        }
        var defaultDatabase = fsql.Select<TenantDatabaseEntity>().First();
        if (defaultDatabase == null)
            fsql.Insert(defaultDatabase = new TenantDatabaseEntity { Label = "体验数据库", DataType = DataType.Sqlite, ConenctionString = "data source={database}.db" }).ExecuteAffrows();
        if (fsql.Select<TenantEntity>().Any(a => a.Id == "main") == false)
            fsql.Insert(new TenantEntity { Id = "main", Host = "localhost", Title = "AdminBlazor SaaS", Description = "AdminBlazor SaaS 系统主库(租户管理)", IsEnabled = true, DatabaseId = defaultDatabase.Id }).ExecuteAffrows();
        
        if (fsql.Select<RoleEntity>().Where(a => a.IsAdministrator).Any() == false)
            fsql.Insert(new RoleEntity { Name = "Administrator", Description = "管理员角色", IsAdministrator = true }).ExecuteAffrows();
        if (fsql.Select<UserEntity>().Where(a => a.Roles.Any(b => b.IsAdministrator)).Any() == false)
        {
            var adminUser = new UserEntity { Username = "admin", Password = "freesql", Nickname = "管理员" };
            adminUser.Roles = [fsql.Select<RoleEntity>().Where(a => a.IsAdministrator).First()];
            fsql.GetAggregateRootRepository<UserEntity>().Insert(adminUser);
        }
        #endregion
        Func<IServiceProvider, IFreeSql> fsqlFactory = r =>
        {
            var admin = r.GetService<AdminContext>();
            return admin.Orm;
        };
        services.AddSingleton(r => cloud);
        services.AddScoped<AdminContext>();
        services.AddScoped(fsqlFactory);
        services.AddScoped<UnitOfWorkManager>();
        services.AddScoped(r => new RepositoryOptions
        {
            AuditValue = e => {
                var user = r.GetService<AdminContext>()?.User;
                if (user == null) return;
                if (e.AuditValueType == AuditValueType.Insert &&
                    e.Object is IEntityCreated obj1 && obj1 != null)
                {
                    obj1.CreatedUserId = user.Id;
                    obj1.CreatedUserName = user.Username;
                }
                if (e.AuditValueType == AuditValueType.Update &&
                    e.Object is IEntityModified obj2 && obj2 != null)
                {
                    obj2.ModifiedUserId = user.Id;
                    obj2.ModifiedUserName = user.Username;
                }
            }
        });
        services.AddScoped(typeof(IBaseRepository<>), typeof(BasicRepository<>));
        services.AddScoped(typeof(BaseRepository<>), typeof(BasicRepository<>));
        services.AddScoped(typeof(IBaseRepository<,>), typeof(BasicRepository<,>));
        services.AddScoped(typeof(BaseRepository<,>), typeof(BasicRepository<,>));
        services.AddScoped(typeof(IAggregateRootRepository<>), typeof(DddRepository<>));

        #region Scheduler, SchedulerAttribute
        Dictionary<string, Action<IServiceProvider, TaskInfo>> schedulerAttributeTriggers = new();
        Func<IServiceProvider, Scheduler> schedulerFactory = r =>
        {
            return new FreeSchedulerBuilder()
                .OnExecuting(task =>
                {
                    if (schedulerAttributeTriggers.TryGetValue(task.Topic, out var trigger))
                    {
                        trigger(r, task);
                        return;
                    }
                    options.SchedulerExecuting?.Invoke(r, task);
                })
                .UseTimeZone(TimeSpan.FromHours(8))
                .UseStorage(fsql)
                .UseCustomInterval(task =>
                {
                    var now = DateTime.UtcNow;
                    var nextTime = NCrontab.CrontabSchedule.Parse(task.IntervalArgument, new NCrontab.CrontabSchedule.ParseOptions { IncludingSeconds = true }).GetNextOccurrence(now);
                    if (nextTime < now) return TimeSpan.FromSeconds(5);
                    return nextTime.Subtract(now);
                })
                .Build();
        };
        if (options.Assemblies.Any())
        {
            foreach (var assembly in options.Assemblies)
            {
                var schedulerMethods = assembly.GetTypes().SelectMany(a => a.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Select(b =>
                {
                    var attr = b.GetCustomAttribute<SchedulerAttribute>();
                    if (attr == null || attr.Name.IsNull()) return null;
                    return new
                    {
                        Class = a,
                        Method = b,
                        IsLazyTask = attr.Argument.IsNull(),
                        TaskInfo = new TaskInfo
                        {
                            Topic = $"[SchedulerAttribute]{attr.Name}",
                            Interval = attr.Interval,
                            IntervalArgument = attr.Argument,
                            Round = attr.Round,
                            Status = attr.Status,
                            Body = "",
                            CreateTime = DateTime.Now,
                            CurrentRound = 0,
                            ErrorTimes = 0,
                            LastRunTime = new DateTime(1970, 1, 1),
                        }
                    };
                })).Where(a => a != null).ToList();
                if (schedulerMethods.Any())
                {
                    var list = fsql.Select<TaskInfo>().Where(a => a.Topic.StartsWith("[SchedulerAttribute]")).ToList();
                    var schedulerTasks = schedulerMethods.Where(a => a.IsLazyTask == false).Select(a => a.TaskInfo).ToList();
                    foreach (var task in schedulerTasks)
                    {
                        var find = list.Find(a => a.Topic == task.Topic);
                        if (find != null)
                        {
                            task.Id = find.Id;
                            task.Body = find.Body;
                            task.CreateTime = find.CreateTime;
                            task.CurrentRound = find.CurrentRound;
                            task.ErrorTimes = find.ErrorTimes;
                            task.LastRunTime = find.LastRunTime;
                        }
                        else
                        {
                            task.Id = $"{DateTime.Now.ToString("yyyyMMdd")}.{YitIdHelper.NextId()}";
                        }
                    }
                    var repo = fsql.GetRepository<TaskInfo>();
                    repo.BeginEdit(list);
                    repo.EndEdit(schedulerTasks);
                    foreach (var schedulerMethod in schedulerMethods)
                    {
                        var method = schedulerMethod.Method;
                        var triggerName = schedulerMethod.TaskInfo.Topic;
                        if (schedulerMethod.IsLazyTask) triggerName = triggerName.Replace("[SchedulerAttribute]", "");
                        schedulerAttributeTriggers[triggerName] = (r, task) =>
                        {
                            var ret = method.Invoke(null, method.GetParameters().Select(a =>
                                a.ParameterType == typeof(IServiceProvider) || a.ParameterType == typeof(ServiceProvider) ? (object)r :
                                a.ParameterType == typeof(TaskInfo) ? task : null).ToArray());
                            if (ret != null && typeof(Task).IsAssignableFrom(method.ReturnType))
                                ((Task)ret)?.Wait();
                        };
                    }
                }
            }
        }
        #endregion
        services.AddSingleton(schedulerFactory);

        services.AddHttpContextAccessor();
        services.AddBootstrapBlazor();
        return services;
    }

    public static string GetQueryStringValue(this NavigationManager nav, string name)
	{
        var obj = HttpUtility.ParseQueryString(new Uri(nav.Uri).Query);
		return obj[name] ?? "";
    }
    public static string[] GetQueryStringValues(this NavigationManager nav, string name)
	{
        var obj = HttpUtility.ParseQueryString(new Uri(nav.Uri).Query);
        return obj.GetValues(name) ?? new string[0];
    }
    public static string UrlEncode(this string str) => str.IsNull() ? "" : HttpUtility.UrlEncode(str);


    record ConfirmResult(bool isConfirmed);
    async public static Task<bool> Confirm(this IJSRuntime JS, string title, string text = "")
    {
        var jsr = await JS.InvokeAsync<ConfirmResult>("Swal.fire", new
        {
            title = title,
            text = text,
            icon = "warning",
            showCancelButton = true,
            confirmButtonColor = "#3085d6",
            cancelButtonColor = "#d33",
            confirmButtonText = "确定",
            cancelButtonText = "取消"
        });
        return jsr.isConfirmed;
    }
    async public static Task Success(this IJSRuntime JS, string title, string text = "")
    {
        await JS.InvokeVoidAsync("Swal.fire", new
        {
            //position = "top-end",
            title = title,
            text = text,
            icon = "success",
            showConfirmButton = false,
            timer = 1500
        });
    }
    async public static Task Error(this IJSRuntime JS, string title, string text = "")
    {
        await JS.InvokeVoidAsync("Swal.fire", new
        {
            //position = "top-end",
            title = title,
            text = text,
            icon = "error",
            showConfirmButton = false,
        });
    }
    async public static Task Warning(this IJSRuntime JS, string title, string text = "")
    {
        await JS.InvokeVoidAsync("Swal.fire", new
        {
            //position = "top-end",
            title = title,
            text = text,
            icon = "warning",
            showConfirmButton = false,
            timer = 1500
        });
    }

    public static List<AdminItem<TItem>> ToAdminItemList<TItem>(this List<TItem> list, IFreeSql fsql)
    {
        var meta = fsql.CodeFirst.GetTableByEntity(typeof(TItem));
        var treeNav = meta.GetAllTableRef().Where(a => a.Value.Exception == null &&
            a.Value.RefType == FreeSql.Internal.Model.TableRefType.OneToMany &&
            a.Value.RefEntityType == typeof(TItem) &&
            a.Value.Columns.Count == 1 &&
            a.Value.Columns[0].Attribute.IsPrimary &&
            a.Value.RefColumns.Count == 1).FirstOrDefault();
        if (treeNav.Key.IsNull() == false)
        {
            var treeList = eachListParentId(treeNav.Value.Columns[0].CsType.CreateInstanceGetDefaultValue(), 1);
            object GetItemPrimaryValue(TItem item) => meta.Primarys[0].GetValue(item);
            List<AdminItem<TItem>> eachListParentId(object id, int level)
            {
                var tempList = new List<AdminItem<TItem>>();
                for (var a = list.Count - 1; a >= 0; a--)
                {
                    var pid = treeNav.Value.RefColumns[0].GetValue(list[a]);
                    if (object.Equals(pid, id))
                    {
                        tempList.Insert(0, new AdminItem<TItem>(list[a]) { Level = level });
                        list.RemoveAt(a);
                    }
                }
                if (list.Any())
                {
                    var tempListLev2 = new List<List<AdminItem<TItem>>>();
                    for (var a = 0; a < tempList.Count; a++)
                        tempListLev2.Add(eachListParentId(GetItemPrimaryValue(tempList[a].Value), level + 1));
                    for (int a = 0, b = 0; a < tempListLev2.Count; a++, b++)
                    {
                        if (tempListLev2[a].Any())
                        {
                            tempList.InsertRange(b + 1, tempListLev2[a]);
                            b += tempListLev2[a].Count;
                        }
                    }
                    tempListLev2.Clear();
                }
                return tempList;
            }
            list.Clear();
            return treeList;
        }
        return list.Select(a => new AdminItem<TItem>(a)).ToList();
    }
}
