class GenRazorOptions
{
    public int curd_PageSize = 20;
    public bool curd_IsRemove = true;
    public bool curd_IsAdd = true;
    public bool curd_IsEdit = true;
    public bool curd_IsMultiSelect = true;
    public bool curd_FormInLine = false;
    public string curd_DialogClassName = "modal-xl";

    public string MenuPath;
    public string MenuLabel;

    public GenTableDesign Design;
    public List<GenTableInfo> AllTables = new();
}

class GenTableInfo
{
    public string TypeNameSpace;
    public string TypeFullName;
    public string TypeName;
    public string CsName;
    public string Comment;
    public List<GenColumnInfo> Columns = new();
}
class GenColumnInfo
{
    public string TypeNameSpace;
    public string TypeName; //GetClassName
    public string MapTypeName;
    public Dictionary<string, long> EnumValues;
    public string CsName;
    public string Comment;
    public bool IsPrimary;
    public bool IsIdentity;
    public bool IsVersion;
    public string DbType;
    public int StringLength;
    public int Precision;
    public int Scale;
}

class GenTableDesign
{
    public GenTableInfo Table;
    public bool IsTreeNav;
    public List<GenColumnDesign> ColumnDesigns { get; set; } = new();
    public List<GenNavigateDesign> OneToOnes = new();
    public List<GenNavigateDesign> ManyToManys = new();
    public List<GenNavigateDesign> OneToManys = new();
}
class GenColumnDesign
{
    public GenColumnInfo Column;
    public int Position;
    public string DisplayText;
    public bool IsDisplay;
    public bool IsDisplayManyToOne;
    public bool CanSearchText;
    public string CanSearchTextTips;
    public bool IsSearchText;
    public bool IsSearchFilterEnum;
    public bool IsSearchFilterManyToOne;
    public bool CanEdit;
    public int EditCol;
    public int EditStyle;
    public GenNavigateDesign ManyToOne;
}
class GenNavigateDesign
{
    public string Name;

    public GenNavigateType RefType;
    public string RefTypeName;
    public string RefMiddleTypeName;

    public List<GenColumnInfo> Columns = new();
    public List<GenColumnInfo> MiddleColumns = new();
    public List<GenColumnInfo> RefColumns = new();

    public string DisplayText;
    public bool IsDisplay;
    public bool IsSearchFilter;
    public bool IsSearchText;
    public int EditStyle;

    public List<GenColumnDesign> RefColumnDesigns;
}
public enum GenNavigateType
{
    OneToOne, ManyToOne, OneToMany, ManyToMany
}
