using FreeSql;
using FreeSql.Internal.Model;
using Microsoft.AspNetCore.Components;
using System.Collections;
namespace BootstrapBlazor.Components;

partial class AllocTable2<TItem, TChild>
{
    /// <summary>
    /// 被分配的对象
    /// </summary>
    [Parameter] public TItem Item { get; set; }
    /// <summary>
    /// 被分配的对象的 List&lt;TChild&gt; 属性
    /// </summary>
    [Parameter] public string ChildProperty { get; set; }
    /// <summary>
    /// 标题
    /// </summary>
    [Parameter] public string Title { get; set; }
    /// <summary>
    /// 分配变化时
    /// </summary>
    [Parameter] public EventCallback<TItem> ItemChanged { get; set; }

    /// <summary>
    /// TChild 分页，值 -1 时不分页
    /// </summary>
    [Parameter] public int PageSize { get; set; } = 20;
    /// <summary>
    /// TChild 开启文本搜索
    /// </summary>
    [Parameter] public bool IsSearchText { get; set; } = true;

    /// <summary>
    /// TChild 表格 TR 模板
    /// </summary>
    [Parameter] public RenderFragment? TableHeader { get; set; }
    /// <summary>
    /// TChild 表格 TD 模板
    /// </summary>
    [Parameter] public RenderFragment<TChild>? TableRow { get; set; }
    [Parameter] public RenderFragment? TableTh1 { get; set; }
    [Parameter] public RenderFragment<TChild>? TableTd1 { get; set; }
    /// <summary>
    /// TChild 正在查询，设置条件
    /// </summary>
    [Parameter] public EventCallback<AdminQueryEventArgs<TChild>> OnQuery { get; set; }

    [Inject] IAggregateRootRepository<TItem> repository { get; set; }

    TableInfo metaTItem;
    TableInfo metaTChild;
    string GetTChildPrimaryValue(TChild child) => metaTChild.Primarys[0].GetValue(child).ConvertTo<string>() ?? "";
    protected override void OnInitialized()
    {
        metaTItem = fsql.CodeFirst.GetTableByEntity(typeof(TItem));
        if (metaTItem.Primarys.Length != 1) throw new ArgumentException("AllocTable2 要求使用类型必须使用单一主键");
        metaTChild = fsql.CodeFirst.GetTableByEntity(typeof(TChild));
        if (metaTItem.Primarys.Length != 1) throw new ArgumentException("AllocTable2 要求使用类型必须使用单一主键");
    }
    async protected override Task OnParametersSetAsync()
    {
        if (Item == null) return;
        if (ChildProperty.IsNull()) return;
        var childs = metaTItem.Properties[ChildProperty].GetValue(Item);
        if (childs == null)
        {
            await new List<TItem> { Item }.IncludeByPropertyNameAsync(repository.Orm, ChildProperty);
            repository.Attach(Item);
            childs = metaTItem.Properties[ChildProperty].GetValue(Item);
        }
        if (childs is IEnumerable childsEnumerable)
        {
            foreach (var child in childsEnumerable)
                if (child is TChild subi) allItems[GetTChildPrimaryValue(subi)] = new AdminItem<TChild>(subi) { Selected = true };
        }
        init = true;
        if (currentItems != null) AllocChanged(currentItems);
    }

    bool init;
    List<AdminItem<TChild>> currentItems;
    Dictionary<string, AdminItem<TChild>> allItems = new();
    void AllocChanged(List<AdminItem<TChild>> e)
    {
        if (currentItems != e || init)
        {
            if (currentItems != e) currentItems = e;
            init = false;
            currentItems.ForEach(a =>
            {
                var pkval = GetTChildPrimaryValue(a.Value);
                a.Selected = allItems.TryGetValue(pkval, out var subi) && subi.Selected;
                allItems[pkval] = a;
            });
            StateHasChanged();
        }
    }
    async Task AllocFinish()
    {
        var childs = metaTItem.Properties[ChildProperty].GetValue(Item) as List<TChild>;
        childs.Clear();
        childs.AddRange(allItems.Values.Where(a => a.Selected).Select(a => a.Value));
        await repository.UpdateAsync(Item);
        await JS.Success("保存成功！");
        await OnClose();
    }

    async Task OnClose()
    {
        if (ItemChanged.HasDelegate) await ItemChanged.InvokeAsync(null);
        currentItems?.Clear();
        currentItems = null;
        allItems.Clear();
        Item = null;
        await Task.Yield();
    }
}