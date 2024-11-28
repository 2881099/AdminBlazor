using AdminBlazor.Infrastructure.Encrypt;
using BootstrapBlazor.Components;
using FreeSql;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System.Reflection;
using Yitter.IdGenerator;

public class AdminContext
{
    public static AdminBlazorOptions AdminBlazorOptions => AdminExtensions.Options;
    public IServiceProvider Service { get; private set; }
    public HttpContext HttpContext { get; }
    NavigationManager Nav;
    IJSRuntime JS;
    FreeSqlCloud cloud;
    public AdminContext(FreeSqlCloud cloud, IHttpContextAccessor httpContextAccessor, NavigationManager nav, IJSRuntime js)
    {
        this.cloud = cloud;
        this.HttpContext = httpContextAccessor.HttpContext;
        this.Service = HttpContext.RequestServices;
        this.Nav = nav;
        this.JS = js;
    }

    public TenantEntity Tenant { get; private set; }
    IFreeSql _orm;
    public IFreeSql Orm
    {
        get
        {
            if (_orm != null) return _orm;
            var tenantHost = HttpContext.Request.Host.Host.ToLower();
            Tenant = cloud.Use("main").Select<TenantEntity>().Where(a => a.Host == tenantHost && a.IsEnabled).First();
            if (Tenant == null) return _orm = cloud.Use("main");
            return _orm = GetTenantFreeSql(Tenant.Id);
        }
    }

    async public Task Init()
    {
        var cookie = HttpContext.Request.Cookies["login"];
        if (!cookie.IsNull())
        {
            if (TryParseCookie(cookie, out var userId, out var loginTime) && userId > 0)
                User = await Orm.Select<UserEntity>().Where(a => a.Id == userId).FirstAsync();
            else
                await SignOut();

            if (User != null && Math.Abs(User.LoginTime.Subtract(loginTime).TotalSeconds) > 1)
            {
                //被其他端登陆挤出
                User = null;
                await SignOut();
                Redirect($"/Admin/Login?Redirect={new Uri(Nav.Uri).PathAndQuery.UrlEncode()}");
                return;
            }
        }
    }
    #region SignIn/Out
    internal string RandToken = Guid.NewGuid().ToString("n");
    async public Task SignIn(UserEntity user, bool remember)
    {
        user.LoginTime = DateTime.Now;
        await Orm.Update<UserEntity>().Where(a => a.Id == user.Id).Set(a => a.LoginTime, user.LoginTime).ExecuteAffrowsAsync();

        if (HttpContext.WebSockets.IsWebSocketRequest)
            await JS.InvokeVoidAsync("adminBlazorJS.setCookie", "login", 
                DesEncrypt.Encrypt(user.Id.ToString() + "|" + user.LoginTime.ToString("yyyy-MM-dd HH:mm:ss")),
                remember ? 15 : -1);
        else
            HttpContext.Response.Cookies.Append("login", DesEncrypt.Encrypt(user.Id.ToString() + "|" + user.LoginTime.ToString("yyyy-MM-dd HH:mm:ss")), new CookieOptions
            {
                Path = "/",
                Expires = remember ? DateTimeOffset.UtcNow.AddDays(15) : null
            });
        await Task.Yield();
    }
    async public Task SignOut()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest) 
            await JS.InvokeVoidAsync("adminBlazorJS.setCookie", "login", "");
        else 
            HttpContext.Response.Cookies.Delete("login");
        await Task.Yield();
    }
    static bool TryParseCookie(string cookie, out long userId, out DateTime loginTime)
    {
        try
        {
            if (!cookie.IsNull())
            {
                var info = DesEncrypt.Decrypt(cookie).Split('|');
                if (info.Length == 2)
                {
                    loginTime = info[1].ConvertTo<DateTime>();
                    userId = info[0].ConvertTo<long>();
                    return true;
                }
            }
        }
        catch
        {
        }
        userId = 0;
        loginTime = DateTime.MinValue;
        return false;
    }
    #endregion

    public void Redirect(string url)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest) Nav.NavigateTo(url, true);
        else HttpContext.Response.Redirect(url);
    }
    public void RedirectLogin()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest) Nav.NavigateTo($"/Admin/Login?Redirect={new Uri(Nav.Uri).PathAndQuery.UrlEncode()}", true);
        else HttpContext.Response.Redirect($"/Admin/Login?Redirect={HttpContext.Request.GetEncodedPathAndQuery().UrlEncode()}");
    }

    public UserEntity User { get; private set; }
    public List<RoleEntity> Roles { get; private set; } = new();
    public List<MenuEntity> RoleMenus { get; private set; } = new();
    DateTime _InitRolesTime;
    async Task InitRoles()
    {
        if (User == null) return;
        if (RoleMenus != null && DateTime.Now.Subtract(_InitRolesTime).TotalSeconds < 60) return;
        _InitRolesTime = DateTime.Now;
        Roles = await Orm.Select<RoleEntity>().Where(a => Orm.Select<RoleUserEntity>().Any(b => b.UserId == User.Id && b.RoleId == a.Id)).ToListAsync();
        if (Roles.Any(a => a.IsAdministrator))
        {
            RoleMenus = await Orm.Select<MenuEntity>().ToListAsync();
            return;
        }
        var roleIds = Roles.Select(a => a.Id);
        RoleMenus = await Orm.Select<MenuEntity>().Where(a => Orm.Select<RoleMenuEntity>()
            .Any(b => roleIds.Contains(b.RoleId) && b.MenuId == a.Id)).ToListAsync();
    }

    public MenuEntity CurrentMenu { get; internal set; }
    public bool AuthPathSuccess { get; internal set; }

    async public Task<bool> AuthPath(string path)
    {
        path = path?.ToLower().Trim('/');
        if (new[]
        {
            "admin/login"
        }.Contains(path)) return true;
        if (User == null) return false;
        await InitRoles();
        if (!Roles.Any()) return false;
        CurrentMenu = RoleMenus.Where(a => a.PathLower == path).FirstOrDefault();

        if (CurrentMenu == null && Roles.Any() && new[]
        {
            "admin",
        }.Contains(path)) return AuthPathSuccess = true;
        if (CurrentMenu != null) CurrentMenu.Parent = RoleMenus.Where(a => a.Id == CurrentMenu.ParentId)?.FirstOrDefault();
        return AuthPathSuccess = CurrentMenu != null;
    }
    async public Task<bool> AuthButton(string path)
    {
        if (User == null) return false;
        if (CurrentMenu == null) return false;
        await InitRoles();
        var pathLower = path.ToLower();
        MenuEntity button = null;
        FindButton(new List<MenuEntity> { CurrentMenu });
        if (button == null)
        {
            if (Tenant.Id == "main")
            {
                await FindButtonFromDB(new List<MenuEntity> { CurrentMenu });
                if (button == null)
                {
                    await Orm.Insert(new MenuEntity
                    {
                        ParentId = CurrentMenu.Id,
                        Label = path,
                        Path = path,
                        Sort = 10031,
                        Type = MenuEntityType.按钮,
                    }).ExecuteAffrowsAsync();
                    _InitRolesTime = DateTime.MinValue;
                }
                if (Roles.Any(a => a.IsAdministrator)) return true;
            }
            return false;
        }
        return true;

        void FindButton(List<MenuEntity> findMenus)
        {
            var parentIds = findMenus.Select(a => a.Id).ToArray();
            var childs = RoleMenus.Where(a => findMenus.Select(a => a.Id).Contains(a.ParentId)).ToList();
            if (childs.Any() == false) return;
            button = childs.Where(a => a.Type == MenuEntityType.按钮 && a.PathLower == pathLower).FirstOrDefault();
            if (button != null) return;
            FindButton(childs);
        }
        async Task FindButtonFromDB(List<MenuEntity> findMenus)
        {
            var parentIds = findMenus.Select(a => a.Id).ToArray();
            var childs = await Orm.Select<MenuEntity>().Where(a => parentIds.Contains(a.ParentId)).ToListAsync();
            if (childs.Any() == false) return;
            button = childs.Where(a => a.Type == MenuEntityType.按钮 && a.PathLower == pathLower).FirstOrDefault();
            if (button != null) return;
            await FindButtonFromDB(childs);
        }
    }

    #region 多页标签
    public List<OpenedTabInfo> OpenedTabs { get; set; } = new(50);
    public class OpenedTabInfo
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public bool IsActive { get; set; }
        [JsonIgnore]
        public bool IsLoad { get; set; }
        [JsonIgnore]
        public Type PageType { get; set; }
    }

    async public Task InitRouteTabs()
    {
        var openedTabJson = await JS.InvokeAsync<string>("adminBlazorJS.getStorage", "adminBlazorOpenedTabs");
        if (string.IsNullOrWhiteSpace(openedTabJson)) openedTabJson = "[]";
        var openedTabs = JsonConvert.DeserializeObject<List<OpenedTabInfo>>(openedTabJson);
        OpenedTabs.AddRange(openedTabs);
    }
    async public Task OpenTab(string key, string title, string url)
    {
        OpenedTabs.ForEach(a => a.IsActive = false);
        var tab = OpenedTabs.FirstOrDefault(a => a.Key == key);
        if (tab == null) OpenedTabs.Add(tab = new OpenedTabInfo { Key = key, Title = title, Url = url });
        var tt = typeof(AdminBlazor.Pages.Dict);
        var uri = Nav.ToAbsoluteUri(tab.Url);
        tab.PageType = AdminBlazorOptions.Assemblies.Select(a => 
            a.GetTypes().FirstOrDefault(b => typeof(ComponentBase).IsAssignableFrom(b) && 
            b.GetCustomAttribute<RouteAttribute>()?.Template == uri.AbsolutePath)).FirstOrDefault(a => a != null);
        tab.IsLoad = true;
        tab.IsActive = true;
        await JS.InvokeVoidAsync("adminBlazorJS.setStorage", "adminBlazorOpenedTabs", JsonConvert.SerializeObject(OpenedTabs));
    }
    #endregion

    #region Modal
    public List<AdminModal> Modals { get; set; }
    async public Task RenderModal()
    {
        await JS.InvokeVoidAsync("adminBlazorJS.modalRender", null);
    }
    #endregion

    #region AuthPath/AuthButton From Database
    //async public Task<bool> AuthPath(string path)
    //{
    //    path = path.ToLower().Trim('/');
    //    if (User == null) return false;
    //    CurrentMenu = await fsql.Select<MenuEntity>().Where(a => a.PathLower == path).FirstAsync();
    //    AuthPathSuccess = false;
    //    if (CurrentMenu == null) return false;
    //    if (!await fsql.Select<RoleUserEntity>().AnyAsync(a => a.UserId == User.Id &&
    //        fsql.Select<RoleMenuEntity>().Any(b => b.MenuId == CurrentMenu.Id && b.RoleId == a.RoleId))) return false;
    //    AuthPathSuccess = true;
    //    return true;
    //}
    //async public Task<bool> AuthButton(string button)
    //{
    //    if (User == null) return false;
    //    if (CurrentMenu == null) return false;
    //    button = button.ToLower();
    //    MenuEntity buttonMenu = null;
    //    //var buttons = new List<MenuEntity>();
    //    await GetAllButtons(new List<MenuEntity> { CurrentMenu });
    //    if (buttonMenu == null) return false;
    //    if (!await fsql.Select<RoleUserEntity>().AnyAsync(a => a.UserId == User.Id &&
    //        fsql.Select<RoleMenuEntity>().Any(b => b.MenuId == buttonMenu.Id && b.RoleId == a.RoleId))) return false;
    //    return true;

    //    async Task GetAllButtons(List<MenuEntity> findMenus)
    //    {
    //        var parentIds = findMenus.Select(a => a.Id).ToArray();
    //        var childs = await fsql.Select<MenuEntity>().Where(a => parentIds.Contains(a.ParentId)).ToListAsync();
    //        if (childs.Any() == false) return;
    //        buttonMenu = childs.Where(a => a.Type == MenuEntityType.按钮 && a.Path == button).FirstOrDefault();
    //        if (buttonMenu != null) return;
    //        //buttons.AddRange(childs);
    //        await GetAllButtons(childs);
    //    }
    //}
    #endregion

    #region 租户
    async internal Task<List<MenuEntity>> GenerateTenantMenus(string tenantId, bool isAdministrator = false)
{
    var main = cloud.Use("main");
    var tenantMenuIds = isAdministrator ? null : await main.Select<TenantMenuEntity>().Where(a => a.TenantId == tenantId).ToListAsync(a => a.MenuId);
    var allMenus = (await main.Select<MenuEntity>().ToListAsync()).ToAdminItemList(main);
    var menus = new List<MenuEntity>();
    var sysMenus = new[] { "admin/user", "admin/role" };
    var level = 0;
    for (var a = 0; a < allMenus.Count; a++)
    {
        if (allMenus[a].Level == 1 && allMenus[a].Value.Label == "系统管理")
        {
            menus.Add(allMenus[a].Value);
            a++;
            for (; a < allMenus.Count && allMenus[a].Level > 1; a++)
            {
                if (sysMenus.Contains(allMenus[a].Value.PathLower))
                {
                    if (isAdministrator || tenantMenuIds.Contains(allMenus[a].Value.Id))
                        menus.Add(allMenus[a].Value);
                    level = allMenus[a].Level;
                    a++;
                    for (; a < allMenus.Count && allMenus[a].Level > level; a++)
                        if (isAdministrator || tenantMenuIds.Contains(allMenus[a].Value.Id))
                            menus.Add(allMenus[a].Value);
                    a--;
                }
            }
            a--;
            continue;
        }
        if (isAdministrator || tenantMenuIds.Contains(allMenus[a].Value.Id))
            menus.Add(allMenus[a].Value);
        //level = allMenus[a].Level;
        //a++;
        //for (; a < allMenus.Count && allMenus[a].Level > level; a++)
        //    if (isAdministrator || tenantMenuIds.Contains(allMenus[a].Value.Id))
        //        menus.Add(allMenus[a].Value);
        //a--;
    }
    return menus;
}
public IFreeSql GetTenantFreeSql(string tenantId)
{
    cloud.Register(tenantId, () =>
    {
        var database = cloud.Use("main").Select<TenantEntity>().Where(a => a.Id == tenantId).First(a => a.Database);
        if (database == null) throw new Exception("租户数据库错误");
        var fsql = new FreeSqlBuilder()
            .UseConnectionString(database.DataType, database.ConenctionString.Replace("{database}", tenantId))
            .UseAdoConnectionPool(true)
            .UseAutoSyncStructure(true)
            .Build();
        AdminContext.ConfigFreeSql(fsql);
        return fsql;
    });
    return cloud.Use(tenantId);
}
internal static void ConfigFreeSql(IFreeSql fsql)
{
    var serverTime = fsql.Ado.QuerySingle(() => DateTime.UtcNow);
    var timeOffset = DateTime.UtcNow.Subtract(serverTime);
    fsql.Aop.AuditValue += (_, e) =>
    {
        if (e.Column.Table.Type == typeof(TenantEntity) && e.Column.CsName == nameof(TenantEntity.Id))
        {
            e.Value = e.Column.Table.ColumnsByCs[nameof(TenantEntity.Id)].GetValue(e.Object).ConvertTo<string>()?.ToLower();
            return;
        }
        if (e.Column.CsName == nameof(MenuEntity.PathLower) && typeof(MenuEntity).IsAssignableFrom(e.Column.Table.Type))
        {
            var path = e.Column.Table.ColumnsByCs[nameof(MenuEntity.Path)].GetValue(e.Object).ConvertTo<string>()?.Trim('/');
            e.Column.Table.ColumnsByCs[nameof(MenuEntity.Path)].SetValue(e.Object, path);
            e.Value = path?.ToLower();
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
}
#endregion

}
