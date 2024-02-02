using AdminBlazor.Infrastructure.Encrypt;
using BootstrapBlazor.Components;
using FreeSql;
using Microsoft.Extensions.DependencyInjection;

public class AdminLoginInfo
{
    public IServiceProvider Service { get; internal set; }
    public UserEntity User { get; set; }
    public List<RoleEntity> Roles { get; private set; }
    public List<MenuEntity> RoleMenus { get; private set; }
    DateTime _InitRolesTime;
    async Task InitRoles()
    {
        if (User == null) return;
        if (RoleMenus != null && DateTime.Now.Subtract(_InitRolesTime).TotalSeconds < 60) return;
        _InitRolesTime = DateTime.Now;
        Roles = await fsql.Select<RoleEntity>().Where(a => fsql.Select<RoleUserEntity>().Any(b => b.UserId == User.Id && b.RoleId == a.Id)).ToListAsync();
        if (Roles.Any(a => a.IsAdministrator))
        {
            RoleMenus = await fsql.Select<MenuEntity>().ToListAsync();
            return;
        }
        var roleIds = Roles.Select(a => a.Id).ToArray();
        RoleMenus = await fsql.Select<MenuEntity>().Where(a => fsql.Select<RoleMenuEntity>().Any(b => roleIds.Contains(b.RoleId) && b.MenuId == a.Id)).ToListAsync();
    }

    IFreeSql _fsql;
    IFreeSql fsql => _fsql ?? (_fsql = Service.GetService<IFreeSql>());

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
            await FindButtonFromDB(new List<MenuEntity> { CurrentMenu });
            if (button == null)
            {
                await fsql.Insert(new MenuEntity
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
            var childs = await fsql.Select<MenuEntity>().Where(a => parentIds.Contains(a.ParentId)).ToListAsync();
            if (childs.Any() == false) return;
            button = childs.Where(a => a.Type == MenuEntityType.按钮 && a.PathLower == pathLower).FirstOrDefault();
            if (button != null) return;
            await FindButtonFromDB(childs);
        }
    }

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

    public static bool TryParseCookie(string cookie, out long userId, out DateTime loginTime)
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
}
