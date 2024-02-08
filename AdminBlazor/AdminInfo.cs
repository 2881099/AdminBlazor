using BootstrapBlazor.Components;
using FreeSql;

namespace BootstrapBlazor.Components
{
    public class AdminItem<T>
    {
        public T Value { get; set; }
        public bool Selected { get; set; }
        public int Level { get; set; } = 1;
        public string RowClass { get; set; }

        public AdminItem(T item) => Value = item;
    }
    public record AdminQueryEventArgs<TItem>(ISelect<TItem> Select, string SearchText, AdminFilterInfo[] Filters);
    public class AdminQueryInfo
    {
        public string SearchText { get; set; }
        public AdminFilterInfo[] Filters { get; set; }
        long _total;
        public long Total
        {
            get => _total;
            set
            {
                if (value < 0) value = 0;
                if (value != _total)
                {
                    _total = value;
                    MaxPageNumber = (int)Math.Ceiling(1.0 * _total / Math.Max(1, PageSize));
                    if (_pageNumber > MaxPageNumber)
                        _pageNumber = MaxPageNumber;
                }
            }
        }
        int _pageNumber = 1;
        public int PageNumber
        {
            get => _pageNumber;
            set
            {
                if (value <= 0) value = 1;
                _pageNumber = value;
            }
        }
        public int PageSize { get; set; } = 20;
        public int MaxPageNumber { get; private set; }
        public string PageNumberQueryStringName { get; set; } = "page";
        public string SearchTextQueryStringName { get; set; } = "search";
        public bool IsQueryString { get; set; } = true;
        public Func<Task> InvokeQueryAsync { get; set; }
    }
    public class AdminFilterInfo
    {
        public string Label { get; set; }
        public string QueryStringName { get; set; }
        public bool Multiple { get; set; }
        public AdminItem<KeyValuePair<string, string>>[] Options { get; set; }
        public int Col { get; set; } = 12;
        public bool HasValue => Options.Where(a => a.Selected).Any();
        public T[] Values<T>() => Options.Where(a => a.Selected).Select(a => a.Value.Value.ConvertTo<T>()).ToArray();
        public T Value<T>() => Values<T>().FirstOrDefault();

        public AdminFilterInfo(string label, string queryStringName, string texts, string values) : this(label, queryStringName, false, 12, texts, values) { }
        public AdminFilterInfo(string label, string queryStringName, bool multiple, int col, string texts, string values)
        {
            Label = label;
            QueryStringName = queryStringName;
            Multiple = multiple;
            var keys = texts.Split(',');
            var vals = values.Split(",");
            if (keys.Length != vals.Length) throw new Exception("texts.Split(',').Length != values.Split(',').Length");
            Options = texts.Split(',').Select((a, b) => new AdminItem<KeyValuePair<string, string>>(KeyValuePair.Create(a.Trim(), vals[b].Trim()))).Where(a => !a.Value.Key.IsNull()).ToArray();
            Col = col;
        }
    }

    public class AdminRemoveEventArgs<TItem>
    {
        public List<TItem> Items { get; set; }
        public bool Cancel { get; set; }
    }
}
