using FreeSql.Extensions.EntityUtil;
using FreeSql.Internal.Model;
using FreeSql;
using Microsoft.AspNetCore.Components;
using System.Collections;

namespace BootstrapBlazor.Components;

partial class AdminTable2<TItem>
{

    [Inject] IAggregateRootRepository<TItem> repository { get; set; }
    IFreeSql fsql => repository.Orm;

    /// <summary>
    /// 打开UI调试
    /// </summary>
    [Parameter] public bool IsDebug { get; set; }
    /// <summary>
    /// 标题，弹框时
    /// </summary>
    [Parameter] public string Title { get; set; }
    /// <summary>
    /// 分页，值 -1 时不分页
    /// </summary>
    [Parameter] public int PageSize { get; set; } = 20;
    /// <summary>
    /// 查询条件与 URL QueryString 同步
    /// </summary>
    [Parameter] public bool IsQueryString { get; set; } = true;
    /// <summary>
    /// 开启删除
    /// </summary>
    [Parameter] public bool IsRemove { get; set; } = true;
    /// <summary>
    /// 开启删除（表格每行）
    /// </summary>
    [Parameter] public bool IsRowRemove { get; set; } = true;
    /// <summary>
    /// 开启添加
    /// </summary>
    [Parameter] public bool IsAdd { get; set; } = true;
    /// <summary>
    /// 开启编辑
    /// </summary>
    [Parameter] public bool IsEdit { get; set; } = true;
    /// <summary>
    /// 开启刷新
    /// </summary>
    [Parameter] public bool IsRefersh { get; set; } = true;
    /// <summary>
    /// 开启文本搜索
    /// </summary>
    [Parameter] public bool IsSearchText { get; set; } = true;
    /// <summary>
    /// 开启单选
    /// </summary>
    [Parameter] public bool IsSingleSelect { get; set; } = false;
    /// <summary>
    /// 开启多选
    /// </summary>
    [Parameter] public bool IsMultiSelect { get; set; } = true;
    /// <summary>
    /// 开启编辑保存时，弹框确认
    /// </summary>
    [Parameter] public bool IsConfirmEdit { get; set; } = true;
    /// <summary>
    /// 开启删除时，弹框确认
    /// </summary>
    [Parameter] public bool IsConfirmRemove { get; set; } = true;
    /// <summary>
    /// 表格一行显示几条记录
    /// </summary>
    [Parameter] public int Colspan { get; set; } = 4;
    /// <summary>
    /// 表格高度
    /// </summary>
    [Parameter] public int BodyHeight { get; set; } = -1;
    /// <summary>
    /// 弹框样式
    /// </summary>
    [Parameter] public string DialogClassName { get; set; }

    /// <summary>
    /// 表格 TR 模板
    /// </summary>
    [Parameter] public RenderFragment? TableHeader { get; set; }
    /// <summary>
    /// 表格 TD 模板
    /// </summary>
    [Parameter] public RenderFragment<TItem>? TableRow { get; set; }
    [Parameter] public RenderFragment? TableTh1 { get; set; }
    [Parameter] public RenderFragment<TItem>? TableTd1 { get; set; }
    [Parameter] public int TableTd99Width { get; set; } = 160;
    [Parameter] public RenderFragment<TItem>? TableTd99 { get; set; }
    /// <summary>
    /// 添加/编辑 模板
    /// </summary>
    [Parameter] public RenderFragment<TItem>? EditTemplate { get; set; }
    /// <summary>
    /// 卡片 Header 模板
    /// </summary>
    [Parameter] public RenderFragment? CardHeader { get; set; }
    /// <summary>
    /// 卡片 Fotter 模板
    /// </summary>
    [Parameter] public RenderFragment? CardFooter { get; set; }

    /// <summary>
    /// 初始化查询
    /// </summary>
    [Parameter] public Func<AdminQueryInfo, Task> InitQuery { get; set; }
    /// <summary>
    /// 正在查询，设置条件
    /// </summary>
    [Parameter] public EventCallback<AdminQueryEventArgs<TItem>> OnQuery { get; set; }
    /// <summary>
    /// 正在编辑，设置编辑对象
    /// </summary>
    [Parameter] public EventCallback<TItem> OnEdit { get; set; }
    /// <summary>
    /// 编辑完成
    /// </summary>
    [Parameter] public EventCallback<TItem> OnEditFinish { get; set; }
    /// <summary>
    /// 正在删除
    /// </summary>
    [Parameter] public EventCallback<List<TItem>> OnRemove { get; set; }
    /// <summary>
    /// 选择内容发生变化
    /// </summary>
    [Parameter] public EventCallback<List<AdminItem<TItem>>> OnSelectChanged { get; set; }
    /// <summary>
    /// 单击表格行时
    /// </summary>
    [Parameter] public EventCallback<AdminItem<TItem>> OnRowClick { get; set; }

    AdminQueryInfo q = new();
    TItem item;
    bool itemIsEdit = false;
    List<AdminItem<TItem>> items = new();
    string tempid;
    TableInfo metaTItem;
    object GetItemPrimaryValue(TItem item) => metaTItem.Primarys[0].GetValue(item);
    KeyValuePair<string, TableRef> treeNav;
    protected override void OnInitialized()
    {
        tempid = $"AdminTable2_{Guid.NewGuid().ToString("n")}";
        metaTItem = fsql.CodeFirst.GetTableByEntity(typeof(TItem));
        if (metaTItem.Primarys.Length != 1) throw new ArgumentException("AdminTable2 要求使用类型必须使用单一主键");
        if (Title.IsNull()) Title = metaTItem.Comment;
        q.PageSize = PageSize;
        q.IsQueryString = IsQueryString;
        q.Filters = new AdminFilterInfo[0];
        treeNav = metaTItem.GetAllTableRef().Where(a => a.Value.Exception == null &&
            a.Value.RefType == TableRefType.OneToMany &&
            a.Value.RefEntityType == typeof(TItem) &&
            a.Value.Columns.Count == 1 &&
            a.Value.Columns[0].Attribute.IsPrimary &&
            a.Value.RefColumns.Count == 1).FirstOrDefault();
        if (treeNav.Key.IsNull() == false) q.PageSize = -1;
    }

    async protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        if (InitQuery != null)
        {
            await InitQuery(q);
            if (q.IsQueryString)
            {
                foreach (var filter in q.Filters)
                {
                    var query = nav.GetQueryStringValues(filter.QueryStringName);
                    foreach (var qval in query)
                    {
                        for (var x = 0; x < filter.Options.Length; x++)
                            if (filter.Options[x].Value.Value == qval)
                                filter.Options[x].Selected = true;
                    }
                }
            }
        }
        q.InvokeQueryAsync = this.Load;
        await q.InvokeQueryAsync();
    }

    async Task Load()
    {
        var select = repository.Select;
        if (OnQuery.HasDelegate) await OnQuery.InvokeAsync(new AdminQueryEventArgs<TItem>(select, q.SearchText, q.Filters));
        if (!IsEdit) select.NoTracking(); //多层弹框可能造成 OnEdit 级联加载内容失效
        if (q.PageSize > 0)
        {
            q.Total = await select.CountAsync();
            select.Page(q.PageNumber, q.PageSize);
        }
        var list = await select.ToListAsync();
        if (q.PageSize <= 0)
        {
            q.Total = list.Count;
        }
        items.Clear();
        items = list.ToAdminItemList(fsql);
        if (OnSelectChanged.HasDelegate) await OnSelectChanged.InvokeAsync(items);
        StateHasChanged();
    }

    [AdminButton("AdminTable2_Refersh")]
    Task Refersh() => Load();
    [AdminButton("remove")]
    async Task Delete(TItem item = null)
    {
        var list = item != null ? new List<TItem> { item } : items.Where(a => a.Selected).Select(a => a.Value).ToList();
        if (list.Any() == false) return;
        if (IsConfirmRemove && await JS.Confirm($"确定要删除 {list.Count}行 记录吗？", "删除之后无法恢复！") == false) return;
        if (OnRemove.HasDelegate) await OnRemove.InvokeAsync(list);
        q.Total -= await repository.DeleteAsync(list);
        await Load();
    }
    [AdminButton("add")]
    async Task BeginAdd()
    {
        item = new();
        itemIsEdit = false;
        if (OnEdit.HasDelegate) await OnEdit.InvokeAsync(item);
    }
    [AdminButton("edit")]
    async Task BeginEdit(TItem editItem)
    {
        item = new();

        AggregateRootUtils.MapEntityValue(null, fsql, typeof(TItem), editItem, item); //注意：此方法只处理 OneToOne/OneToMany 属性
        foreach (var nav in fsql.CodeFirst.GetTableByEntity(typeof(TItem)).GetAllTableRef()
            .Where(a => a.Value.Exception == null && a.Value.RefType == TableRefType.ManyToOne))
            EntityUtilExtensions.SetEntityValueWithPropertyName(fsql, typeof(TItem), item, nav.Key,
                EntityUtilExtensions.GetEntityValueWithPropertyName(fsql, typeof(TItem), editItem, nav.Key));

        foreach (var nav in fsql.CodeFirst.GetTableByEntity(typeof(TItem)).GetAllTableRef()
            .Where(a => a.Value.Exception == null && a.Value.RefType == TableRefType.OneToOne)) //1对1 同时编辑
        {
            var fromObj = EntityUtilExtensions.GetEntityValueWithPropertyName(fsql, typeof(TItem), editItem, nav.Key);
            if (fromObj == null) continue;
            var toObj = EntityUtilExtensions.GetEntityValueWithPropertyName(fsql, typeof(TItem), item, nav.Key);
            foreach (var nav2 in fsql.CodeFirst.GetTableByEntity(nav.Value.RefEntityType).GetAllTableRef()
                .Where(a => a.Value.Exception == null && a.Value.RefType == TableRefType.ManyToOne))
                EntityUtilExtensions.SetEntityValueWithPropertyName(fsql, nav.Value.RefEntityType, toObj, nav2.Key,
                    EntityUtilExtensions.GetEntityValueWithPropertyName(fsql, nav.Value.RefEntityType, fromObj, nav2.Key));
        }

        foreach (var nav in fsql.CodeFirst.GetTableByEntity(typeof(TItem)).GetAllTableRef()
            .Where(a => a.Value.Exception == null && a.Value.RefType == TableRefType.OneToMany)) //1对多 同时编辑
        {
            var fromEach = EntityUtilExtensions.GetEntityValueWithPropertyName(fsql, typeof(TItem), editItem, nav.Key) as IEnumerable;
            if (fromEach == null) continue;
            var toEach = EntityUtilExtensions.GetEntityValueWithPropertyName(fsql, typeof(TItem), item, nav.Key) as IEnumerable;
            var fromList = new List<object>();
            var toList = new List<object>();
            foreach (var obj in fromEach) fromList.Add(obj);
            foreach (var obj in toEach) toList.Add(obj);
            if (fromList.Count != toList.Count) continue;

            foreach (var nav2 in fsql.CodeFirst.GetTableByEntity(nav.Value.RefEntityType).GetAllTableRef()
                .Where(a => a.Value.Exception == null && a.Value.RefType == TableRefType.ManyToOne))
                for (var a = 0; a < fromList.Count; a++)
                    EntityUtilExtensions.SetEntityValueWithPropertyName(fsql, nav.Value.RefEntityType, toList[a], nav2.Key,
                       EntityUtilExtensions.GetEntityValueWithPropertyName(fsql, nav.Value.RefEntityType, fromList[a], nav2.Key));
        }

        itemIsEdit = true;
        if (OnEdit.HasDelegate) await OnEdit.InvokeAsync(item);
    }
    async Task Save()
    {
        if (itemIsEdit)
        {
            if (IsConfirmEdit && await JS.Confirm($"确定要修改数据吗？") == false) return;
            await repository.UpdateAsync(item);
        }
        else
        {
            await repository.InsertAsync(item);
        }
        if (OnEditFinish.HasDelegate) await OnEditFinish.InvokeAsync(item);
        item = null;
        await Load();
    }

    async Task RowClick(AdminItem<TItem> opt)
    {
        if (OnSelectChanged.HasDelegate)
        {
            if (opt == null) return;
            await CheckboxOnChange(opt, !opt.Selected);
        }
        if (OnRowClick.HasDelegate)
        {
            if (opt == null) return;
            await OnRowClick.InvokeAsync(opt);
        }
    }

    object selectedSingleValue;
    async Task CheckboxOnChange(AdminItem<TItem> opt, bool selected)
    {
        if (opt == null) //all
        {
            for (var a = 0; a < items.Count; a++)
                items[a].Selected = selected;
        }
        else //single
        {
            if (IsSingleSelect)
            {
                var optPkValue = GetItemPrimaryValue(opt.Value);
                TItem tmp = null;
                items.ForEach(a =>
                {
                    a.Selected = object.Equals(GetItemPrimaryValue(a.Value), optPkValue) ? true : false;
                    if (a.Selected) tmp = a.Value;
                });
                selectedSingleValue = optPkValue;
            }
            else
            {
                opt.Selected = selected;
                for (var a = 0; a < items.Count; a++)
                {
                    if (items[a] == opt)
                    {
                        for (var b = a + 1; b < items.Count; b++)
                        {
                            if (items[b].Level > opt.Level) items[b].Selected = opt.Selected;
                            else break;
                        }
                        break;
                    }
                }
                for (var a = 0; a < items.Count; a++)
                {
                    var rootMenu = items[a];
                    var selectedCount = 0;
                    if (rootMenu.Level == 1)
                    {
                        a++;
                        for (; a < items.Count && items[a].Level > rootMenu.Level; a++)
                            if (items[a].Selected) selectedCount++;
                        a--;
                        rootMenu.Selected = selectedCount > 0;
                    }
                }
            }
        }
        if (OnSelectChanged.HasDelegate) await OnSelectChanged.InvokeAsync(items);
    }

}