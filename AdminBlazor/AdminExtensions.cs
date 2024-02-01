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
    internal string BaseUrl { get; set; } = "/Admin";
    public string Title { get; set; } = "Admin Blazor";
    public Assembly[] Assemblies { get; set; }
    public Action<IServiceProvider, TaskInfo> SchedulerExecuting { get; set; }
    public Action<FreeSqlBuilder> FreeSqlBuilder { get; set; }

    internal static string Global_GeneartorKey;
    internal static string Global_GeneartorServer;
    internal static string Global_BaseUrl;
    internal static string Global_Title;
}

public static class AdminExtensions
{

    public static IServiceCollection AddAdminBlazor(this IServiceCollection services, AdminBlazorOptions options)
    {
        if (options == null) options = new AdminBlazorOptions();
        AdminBlazorOptions.Global_GeneartorKey = options.GeneartorKey;
        AdminBlazorOptions.Global_GeneartorServer = options.GeneartorServer.TrimEnd('/');
        AdminBlazorOptions.Global_BaseUrl = options.BaseUrl.TrimEnd('/');
        AdminBlazorOptions.Global_Title = options.Title;
        if (options.Assemblies == null) options.Assemblies = new[] { typeof(AdminExtensions).Assembly };
        else options.Assemblies = options.Assemblies.Concat(new[] { typeof(AdminExtensions).Assembly }).Distinct().ToArray();
        Func<IServiceProvider, IFreeSql> fsqlFactory = r =>
        {
            var fsqlBuilder = new FreeSqlBuilder()
                .UseNoneCommandParameter(true);
            if (options.FreeSqlBuilder != null) options.FreeSqlBuilder?.Invoke(fsqlBuilder);
            else fsqlBuilder
                .UseConnectionString(DataType.Sqlite, @"Data Source=freedb.db")
                .UseMonitorCommand(cmd => System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {cmd.CommandText}\r\n"))//监听SQL语句
                .UseAutoSyncStructure(true); //自动同步实体结构到数据库，FreeSql不会扫描程序集，只有CRUD时才会生成表。
            IFreeSql fsql = fsqlBuilder.Build();
            YitIdHelper.SetIdGenerator(new IdGeneratorOptions(options.WorkId) { WorkerIdBitLength = 6 });
            var serverTime = fsql.Ado.QuerySingle(() => DateTime.UtcNow);
            var timeOffset = DateTime.UtcNow.Subtract(serverTime);
            fsql.Aop.AuditValue += (_, e) =>
            {
                if (e.Column.CsName == nameof(MenuEntity.PathLower) && typeof(MenuEntity).IsAssignableFrom(e.Column.Table.Type))
                {
                    e.Value = e.Column.Table.ColumnsByCs[nameof(MenuEntity.Path)].GetValue(e.Object).ConvertTo<string>()?.ToLower();
                    return;
                }

                //数据库时间
                if ((e.Column.CsType == typeof(DateTime) || e.Column.CsType == typeof(DateTime?))
                    && e.Column.Attribute.ServerTime != DateTimeKind.Unspecified
                    && (e.Value == null || (DateTime)e.Value == default || (DateTime?)e.Value == default))
                {
                    e.Value = (e.Column.Attribute.ServerTime == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now).Subtract(timeOffset);
                    return;
                }

                //雪花Id
                if (e.Column.CsType == typeof(long)
                    && e.Property.GetCustomAttribute<SnowflakeAttribute>(false) != null
                    && (e.Value == null || (long)e.Value == default || (long?)e.Value == default))
                {
                    e.Value = YitIdHelper.NextId();
                    return;
                }
            };

            #region 初始化数据
            if (fsql.Select<MenuEntity>().Any() == false)
            {
                List<MenuEntity> getCudButtons(params MenuEntity[] btns) => new[]
                {
                    new MenuEntity { Label = "添加", Path = "add", Sort = 10011, Type = MenuEntityType.按钮, },
                    new MenuEntity { Label = "编辑", Path = "edit", Sort = 10012, Type = MenuEntityType.按钮, },
                    new MenuEntity { Label = "删除", Path = "remove", Sort = 10013, Type = MenuEntityType.按钮, }
                }.Concat(btns ?? new MenuEntity[0]).ToList();
                var repo = fsql.GetAggregateRootRepository<MenuEntity>();
                repo.Insert(new[]
                {
                    new MenuEntity
                    {
                        Label = "系统管理",
                        Path = "",
                        Sort = 1000,
                        Type = MenuEntityType.菜单,
                        Childs = new List<MenuEntity>
                        {
                            new MenuEntity { Label = "菜单管理", Path = "Admin/Menu", Sort = 10001, Type = MenuEntityType.菜单, Childs = getCudButtons() },
                            new MenuEntity { Label = "角色管理", Path = "Admin/Role", Sort = 10002, Type = MenuEntityType.菜单, Childs = getCudButtons(
                                new MenuEntity { Label = "分配用户", Path = "alloc_users", Sort = 10014, Type = MenuEntityType.按钮, },
                                new MenuEntity { Label = "分配菜单", Path = "alloc_menus", Sort = 10015, Type = MenuEntityType.按钮, })
                            },
                            new MenuEntity { Label = "用户管理", Path = "Admin/User", Sort = 10003, Type = MenuEntityType.菜单, Childs = getCudButtons(
                                new MenuEntity { Label = "分配角色", Path = "alloc_roles", Sort = 10014, Type = MenuEntityType.按钮, })
                            },
                            new MenuEntity { Label = "定时任务", Path = "Admin/TaskScheduler", Sort = 10004, Type = MenuEntityType.菜单, Childs = new List<MenuEntity>
                                {
                                    new MenuEntity { Label = "添加", Path = "add", Sort = 10011, Type = MenuEntityType.按钮, },
                                    new MenuEntity { Label = "删除", Path = "remove", Sort = 10013, Type = MenuEntityType.按钮, },
                                    new MenuEntity { Label = "暂停", Path = "pause", Sort = 10014, Type = MenuEntityType.按钮, },
                                    new MenuEntity { Label = "恢复", Path = "resume", Sort = 10015, Type = MenuEntityType.按钮, },
                                    new MenuEntity { Label = "立即触发", Path = "runnow", Sort = 10016, Type = MenuEntityType.按钮, },
                                    new MenuEntity { Label = "查看日志", Path = "tasklog", Sort = 10017, Type = MenuEntityType.按钮, },
                                    new MenuEntity { Label = "集群日志", Path = "clusterlog", Sort = 10018, Type = MenuEntityType.按钮, },
                                }
                            },
                            new MenuEntity { Label = "数据字典", Path = "Admin/Dict", Sort = 10005, Type = MenuEntityType.菜单,Childs = getCudButtons() },
                        }
                    },
                });
            }
            if (fsql.Select<RoleEntity>().Where(a => a.IsAdministrator).Any() == false)
                fsql.Insert(new RoleEntity { Name = "Administrator", Description = "管理员角色", IsAdministrator = true }).ExecuteAffrows();
            if (fsql.Select<UserEntity>().Where(a => a.Roles.Any(b => b.IsAdministrator)).Any() == false)
            {
                var adminUser = new UserEntity { Username = "admin", Password = "freesql", Nickname = "管理员" };
                fsql.Insert(adminUser).ExecuteAffrows();
                var adminRole = fsql.Select<RoleEntity>().Where(a => a.IsAdministrator).First();
                fsql.Insert(new RoleUserEntity { UserId = adminUser.Id, RoleId = adminRole.Id }).ExecuteAffrows();
            }
            #endregion

            return fsql;
        };
        services.AddSingleton(fsqlFactory);
        services.AddScoped<UnitOfWorkManager>();
        services.AddScoped(r => new RepositoryOptions
        {
            AuditValue = e => {
                var user = r.GetService<AdminLoginInfo>()?.User;
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
        Dictionary<string, Action<IServiceProvider>> schedulerAttributeTriggers = new();
        Func<IServiceProvider, Scheduler> schedulerFactory = r =>
        {
            return new FreeSchedulerBuilder()
                .OnExecuting(task =>
                {
                    System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] {task.Topic} 被执行");
                    if (schedulerAttributeTriggers.TryGetValue(task.Topic, out var trigger))
                    {
                        trigger(r);
                        return;
                    }
                    options.SchedulerExecuting?.Invoke(r, task);
                })
                .UseTimeZone(TimeSpan.FromHours(8))
                .UseStorage(r.GetService<IFreeSql>())
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
                var schedulerMethods = assembly.GetTypes().SelectMany(a => a.GetMethods(BindingFlags.Static).Select(b =>
                {
                    var attr = b.GetCustomAttribute<SchedulerAttribute>();
                    if (attr == null || attr.Name.IsNull() || attr.Argument.IsNull()) return null;
                    return new
                    {
                        Class = a,
                        Method = b,
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

                foreach (var pageType in assembly.GetTypes().Where(a => typeof(ComponentBase).IsAssignableFrom(a)))
                {
                    var route = pageType.GetCustomAttribute<RouteAttribute>();
                    if (route == null) continue;
                    var buttonAttrs = new List<string>();
                    GetAllMembers(pageType);
                    void GetAllMembers(Type targetType, Dictionary<Type, bool> ignore = null)
                    {
                        var ignoreIsNull = ignore == null;
                        if (ignoreIsNull) ignore = new();
                        foreach (var member in targetType.GetMembers(BindingFlags.NonPublic))
                        {
                            if (member.MemberType == MemberTypes.Method)
                            {
                                var attr = member.GetCustomAttribute<AdminButtonAttribute>();
                                if (attr?.Name.IsNull() == false)
                                    buttonAttrs.Add(attr.Name);
                                continue;
                            }
                            var memberType = member.GetPropertyOrFieldType();
                            if (memberType == null) continue;
                            if (!typeof(ComponentBase).IsAssignableFrom(memberType)) continue;
                            if (ignore.ContainsKey(memberType)) continue;
                            ignore.Add(memberType, true);
                            GetAllMembers(memberType);
                        }
                        if (ignoreIsNull) ignore.Clear();
                    }
                    if (buttonAttrs.Any())
                    {
                        ;
                    }
                }
                if (schedulerMethods.Any())
                {
                    using (var fsql = fsqlFactory(null))
                    {
                        var list = fsql.Select<TaskInfo>().Where(a => a.Topic.StartsWith("[SchedulerAttribute]")).ToList();
                        foreach (var method in schedulerMethods)
                        {
                            var find = list.Find(a => a.Topic == method.TaskInfo.Topic);
                            if (find != null)
                            {
                                method.TaskInfo.Body = find.Body;
                                method.TaskInfo.CreateTime = find.CreateTime;
                                method.TaskInfo.CurrentRound = find.CurrentRound;
                                method.TaskInfo.ErrorTimes = find.ErrorTimes;
                                method.TaskInfo.LastRunTime = find.LastRunTime;
                            }
                        }
                        var repo = fsql.GetRepository<TaskInfo>();
                        repo.BeginEdit(list);
                        repo.EndEdit(schedulerMethods.Select(a => a.TaskInfo).ToList());
                    }
                    foreach (var method in schedulerMethods)
                        schedulerAttributeTriggers[method.TaskInfo.Topic] = r =>
                            method.Method.Invoke(null, new object[] { r });
                }
            }
        }
        #endregion
        services.AddSingleton(schedulerFactory);

        services.AddHttpContextAccessor();
        services.AddBootstrapBlazor();
        services.AddScoped<AdminLoginInfo>();
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
