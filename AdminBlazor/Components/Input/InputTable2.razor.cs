using FreeSql.Internal.Model;
using Microsoft.AspNetCore.Components;
namespace BootstrapBlazor.Components;

partial class InputTable2<TItem, TKey>
{
    /// <summary>
    /// 值
    /// </summary>
    [Parameter] public TKey Value { get; set; }
    [Parameter] public EventCallback<TKey> ValueChanged { get; set; }
    /// <summary>
    /// 值变化时
    /// </summary>
    [Parameter] public EventCallback<TKey> OnValueChanged { get; set; }
    /// <summary>
    /// 【多对一】导航属性
    /// </summary>
    [Parameter] public TItem Item { get; set; }
    [Parameter] public EventCallback<TItem> ItemChanged { get; set; }
    /// <summary>
    /// 【多对一】导航属性变化时
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemChanged { get; set; }
    /// <summary>
    /// 【多对多】导航属性
    /// </summary>
    [Parameter] public List<TItem> Items { get; set; }
    [Parameter] public EventCallback<List<TItem>> ItemsChanged { get; set; }
    /// <summary>
    /// 【多对多】导航属性变化时
    /// </summary>
    [Parameter] public EventCallback<List<TItem>> OnItemsChanged { get; set; }
    /// <summary>
    /// 文本框显示内容
    /// </summary>
    [Parameter] public Func<TItem, string> DisplayText { get; set; }

    /// <summary>
    /// 弹框标题
    /// </summary>
    [Parameter] public string ModalTitle { get; set; } = "选择..";
    /// <summary>
    /// 弹框显示 TItem 分页，值 -1 时不分页
    /// </summary>
    [Parameter] public int PageSize { get; set; } = 20;
    /// <summary>
    /// 弹框显示 TItem 开启文本搜索
    /// </summary>
    [Parameter] public bool IsSearchText { get; set; } = true;

    /// <summary>
    /// 弹框显示 TItem 表格 TR 模板
    /// </summary>
    [Parameter] public RenderFragment? TableHeader { get; set; }
    /// <summary>
    /// 弹框显示 TItem 表格 TD 模板
    /// </summary>
    [Parameter] public RenderFragment<TItem>? TableRow { get; set; }
    [Parameter] public RenderFragment? TableTh1 { get; set; }
    [Parameter] public RenderFragment<TItem>? TableTd1 { get; set; }
    /// <summary>
    /// 弹框显示 TItem 正在查询，设置条件
    /// </summary>
    [Parameter] public EventCallback<AdminQueryEventArgs<TItem>> OnQuery { get; set; }

    TableInfo metaTItem;
    TKey GetPrimaryValue(TItem item) => metaTItem.Primarys[0].GetValue(item).ConvertTo<TKey>();
    protected override void OnInitialized()
    {
        metaTItem = fsql.CodeFirst.GetTableByEntity(typeof(TItem));
        if (metaTItem.Primarys.Length != 1) throw new ArgumentException("InputTable2 要求使用类型必须使用单一主键");
        if (metaTItem.Primarys[0].CsType.NullableTypeOrThis() != typeof(TKey).NullableTypeOrThis()) throw new ArgumentException("InputTable2 要求使用类型的主键，必须与 TKey 类型相同");
    }

    void OpenModal()
    {
        allItems.Clear();
        if (Item != null)
            allItems[GetPrimaryValue(Item)] = new AdminItem<TItem>(Item) { Selected = true };
        if (Items != null)
            foreach (var item in Items)
                allItems[GetPrimaryValue(item)] = new AdminItem<TItem>(item) { Selected = true };
        showModal = init = true;
        if (currentItems != null) OnSelectChanged(currentItems);
    }

    string ClientId = $"modal-{Guid.NewGuid().ToString("n")}";
    bool showModal, init;
    List<AdminItem<TItem>> currentItems;
    Dictionary<TKey, AdminItem<TItem>> allItems = new();
    void OnSelectChanged(List<AdminItem<TItem>> e)
    {
        if (currentItems != e || init)
        {
            if (currentItems != e) currentItems = e;
            init = false;
            currentItems.ForEach(a =>
            {
                var pkval = GetPrimaryValue(a.Value);
                if (ItemChanged.HasDelegate)
                    a.Selected = object.Equals(GetPrimaryValue(Item), pkval);
                else if (ItemsChanged.HasDelegate)
                    a.Selected = allItems.TryGetValue(pkval, out var subi) && subi.Selected;
                else if (ValueChanged.HasDelegate)
                    a.Selected = object.Equals(Value, pkval);
                allItems[pkval] = a;
            });
            StateHasChanged();
        }
    }
    async Task Finish()
    {
        if (ItemChanged.HasDelegate)
        {
            Item = allItems.Values.Where(a => a.Selected).Select(a => a.Value).FirstOrDefault();
            await ItemChanged.InvokeAsync(Item);
            if (OnItemChanged.HasDelegate) await OnItemChanged.InvokeAsync(Item);
            var pkval = GetPrimaryValue(Item);
            await ValueChanged.InvokeAsync(pkval);
            if (OnValueChanged.HasDelegate) await OnValueChanged.InvokeAsync(pkval);
        }
        else if (ItemsChanged.HasDelegate)
        {
            var ischanged = false;
            if (Items == null)
            {
                Items = new List<TItem>();
                ischanged = true;
            }
            Items.Clear();
            Items.AddRange(allItems.Values.Where(a => a.Selected).Select(a => a.Value));
            if (ischanged)
            {
                await ItemsChanged.InvokeAsync(Items);
                if (OnItemsChanged.HasDelegate) await OnItemsChanged.InvokeAsync(Items);
            }
        }
        else if (ValueChanged.HasDelegate)
        {
            Value = GetPrimaryValue(allItems.Values.Where(a => a.Selected).FirstOrDefault()?.Value);
            await ValueChanged.InvokeAsync(Value);
            if (OnValueChanged.HasDelegate) await OnValueChanged.InvokeAsync(Value);
        }
        OnClose();
    }
    void OnClose()
    {
        showModal = false;
        allItems.Clear();
    }
}
