using FreeSql.DataAnnotations;
using FreeSql.Internal.Model;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;

namespace BootstrapBlazor.Components;

partial class AdminTable2Design
{
    [Parameter] public MenuEntity Menu { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    List<TableInfo> tableDescrptors;
    TableDesign design = new();

    int tabindex = 0;
    int curd_PageSize = 20;
    bool curd_IsRemove = true;
    bool curd_IsAdd = true;
    bool curd_IsEdit = true;
    bool curd_IsMultiSelect = true;
    bool curd_FormInLine = false;
    string curd_DialogClassName = "modal-xl";

    void EntityChanged(ChangeEventArgs e)
    {
        design.TableDescrptor = tableDescrptors.FirstOrDefault(a => a.Type.FullName == e.Value.ConvertTo<string>());
        design.ColumnDesigns.Clear();
        design.OneToOnes.Clear();
        design.ManyToManys.Clear();
        design.OneToManys.Clear();
        design.TreeNav = default;
        if (design.TableDescrptor != null)
        {
            design.TreeNav = design.TableDescrptor.GetAllTableRef().Where(a => a.Value.Exception == null &&
                a.Value.RefType == TableRefType.OneToMany &&
                a.Value.RefEntityType == design.TableDescrptor.Type &&
                a.Value.Columns.Count == 1 &&
                a.Value.Columns[0].Attribute.IsPrimary &&
                a.Value.RefColumns.Count == 1).FirstOrDefault();
            if (design.TreeNav.Key.IsNull() == false)
            {
                curd_PageSize = -1;
            }
            else
            {
                curd_PageSize = 20;
            }

            design.ColumnDesigns.AddRange(GetColumnDesigns(design.TableDescrptor));
            if (design.ColumnDesigns.Count(a => a.CanEdit) >= 36)
            {
                design.ColumnDesigns.ForEach(a => a.EditCol = 2);
                curd_DialogClassName = "modal-fullscreen";
            }
            else if (design.ColumnDesigns.Count(a => a.CanEdit) >= 24)
            {
                design.ColumnDesigns.ForEach(a => a.EditCol = 3);
                curd_DialogClassName = "modal-xxl";
            }
            else if (design.ColumnDesigns.Count(a => a.CanEdit) >= 18)
            {
                design.ColumnDesigns.ForEach(a => a.EditCol = 4);
                curd_DialogClassName = "modal-xl";
            }
            else if (design.ColumnDesigns.Count(a => a.CanEdit) >= 6)
            {
                design.ColumnDesigns.ForEach(a => a.EditCol = 6);
                curd_DialogClassName = "modal-lg";
            }
            else
            {
                curd_DialogClassName = "";
            }
            design.OneToOnes.AddRange(design.TableDescrptor.GetAllTableRef().Where(b => b.Value.Exception == null &&
                b.Value.RefType == TableRefType.OneToOne &&
                b.Value.RefEntityType != design.TableDescrptor.Type &&
                b.Value.Columns.Count == 1).Select(a =>
                {
                    var reftb = fsql.CodeFirst.GetTableByEntity(a.Value.RefEntityType);
                    var refcols = GetColumnDesigns(reftb);
                    refcols.ForEach(cd =>
                    {
                        if (cd.Column.Attribute.IsPrimary)
                            cd.CanEdit = cd.IsDisplay = false;
                    });
                    return new NavigateDesign
                    {
                        Navigate = a,
                        DisplayText = reftb.Comment.IsNull(a.Key),
                        IsDisplay = true,
                        IsSearchFilter = false,
                        EditStyle = 0,
                        RefColumnDesigns = refcols
                    };
                }));
            design.ManyToManys.AddRange(design.TableDescrptor.GetAllTableRef().Where(a => a.Value.Exception == null &&
                a.Value.RefType == TableRefType.ManyToMany &&
                a.Value.Columns.Count == 1 &&
                a.Value.RefColumns.Count == 1).Select(a => new NavigateDesign
                {
                    Navigate = a,
                    DisplayText = fsql.CodeFirst.GetTableByEntity(a.Value.RefEntityType).Comment.IsNull(a.Key),
                    IsDisplay = false,
                    IsSearchFilter = true,
                    EditStyle = 0
                }));
            design.OneToManys.AddRange(design.TableDescrptor.GetAllTableRef().Where(a => a.Value.Exception == null &&
                a.Value.RefType == TableRefType.OneToMany &&
                a.Value.Columns.Count == 1 &&
                a.Value.RefColumns.Count == 1).Select(a =>
                {
                    var reftb = fsql.CodeFirst.GetTableByEntity(a.Value.RefEntityType);
                    var refcols = GetColumnDesigns(reftb);
                    refcols.ForEach(cd =>
                    {
                        if (cd.Column.CsName == a.Value.RefColumns[0].CsName)
                        {
                            cd.CanEdit = cd.IsDisplay = false;
                            cd.ManyToOne = default;
                        }
                        cd.IsDisplay = false;
                        if (cd.CanEdit)
                        {
                            var csType = cd.Column.CsType.NullableTypeOrThis();
                            if (csType.IsNumberType()) cd.EditCol = 60;
                            else if (csType == typeof(string))
                            {
                                if (cd.Column.Attribute.StringLength < 5) cd.EditCol = 60;
                                else if (cd.Column.Attribute.StringLength < 8) cd.EditCol = 80;
                                else if (cd.Column.Attribute.StringLength < 10) cd.EditCol = 100;
                                else if (cd.Column.Attribute.StringLength < 12) cd.EditCol = 120;
                                else cd.EditCol = 150;
                            }
                            else cd.EditCol = 0;
                        }
                    });
                    return new NavigateDesign
                    {
                        Navigate = a,
                        DisplayText = reftb.Comment.IsNull(a.Key),
                        IsDisplay = false,
                        IsSearchFilter = false,
                        EditStyle = -1,
                        RefColumnDesigns = refcols
                    };
                }));
        }
        StateHasChanged();
    }

    List<ColumnDesign> GetColumnDesigns(TableInfo tbdesc) => tbdesc.ColumnsByPosition.Select(a =>
    {
        var cd = new ColumnDesign
        {
            Column = a,
            DisplayText = a.Comment.IsNull(a.CsName),
            IsDisplay = true,
            CanSearchText = design.TreeNav.Key.IsNull() && a.CsType == typeof(string),
            ManyToOne = a.Table.GetAllTableRef().Where(b => b.Value.Exception == null &&
                b.Value.RefType == TableRefType.ManyToOne &&
                //b.Value.RefEntityType != a.Table.Type &&
                b.Value.Columns.Count == 1 &&
                b.Value.Columns[0].CsName == a.CsName).FirstOrDefault(),
            CanEdit = !new[]
            {
                typeof(IEntity<long>),
                typeof(Entity<long>),
                typeof(IEntityCreated),
                typeof(EntityCreated<long>),
                typeof(EntityCreated),
                typeof(IEntityModified),
                typeof(EntityModified<long>),
                typeof(EntityModified),
                typeof(IEntitySoftDelete),
                typeof(EntitySoftDelete<long>),
                typeof(EntitySoftDelete),
                typeof(EntityFull<long>),
                typeof(EntityFull)
            }.Contains(a.Table.Properties[a.CsName].DeclaringType)
        };
        if (a.Attribute.IsPrimary && a.Attribute.MapType.NullableTypeOrThis() == typeof(Guid) ||
            a.Attribute.IsIdentity ||
            a.Attribute.IsVersion) cd.CanEdit = false;

        cd.IsDisplay = cd.CanEdit || new[] { nameof(IEntityCreated.CreatedTime) }.Contains(cd.Column.CsName);
        cd.IsSearchText = cd.CanSearchText;
        if (!cd.IsSearchText) cd.CanSearchTextTips = design.TreeNav.Key.IsNull() ? "非 string 类型无法设置" : "树型分类 暂不支持文本搜索";
        if (cd.Column.CsType.NullableTypeOrThis().IsEnum) cd.IsSearchFilterEnum = true;
        if (!cd.ManyToOne.Key.IsNull())
        {
            if (cd.ManyToOne.Value.RefEntityType != a.Table.Type) cd.IsSearchFilterManyToOne = true;
            cd.EditStyle = 1;
        }
        cd.EditCol = 12;
        return cd;
    }).ToList();

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender) return;
        if (tableDescrptors == null)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a =>
                !a.FullName.StartsWith("System") &&
                !a.FullName.StartsWith("Microsoft") &&
                !a.FullName.StartsWith("FreeSql") &&
                !a.FullName.StartsWith("BootstrapBlazor") &&
                !a.FullName.StartsWith("Rougamo") &&
                !a.FullName.StartsWith("Newtonsoft") &&
                !a.FullName.StartsWith("Swashbuckle") &&
                !a.FullName.StartsWith("NCrontab") &&
                !a.FullName.StartsWith("Yitter")).ToArray();
            tableDescrptors = new();
            tableDescrptors.AddRange(assemblies.SelectMany(assembly => assembly.GetTypes().Where(type =>
                type.IsAbstract == false &&
                type.IsGenericType == false &&
                (
                    (type.GetInterfaces().Any(i => i.IsGenericType && typeof(IEntity<>).IsAssignableFrom(i.GetGenericTypeDefinition()))) ||
                    new Type[]
                    {
                        //typeof(TaskInfo)
                    }.Contains(type) ||
                    type.GetCustomAttribute<TableAttribute>() != null
                ))
                .Distinct().OrderBy(type => type.FullName)
                .Select(type => fsql.CodeFirst.GetTableByEntity(type))
                .Where(tb => tb != null)
                .ToArray()));
        }
    }

    class TableDesign
    {
        public TableInfo TableDescrptor { get; set; }
        public List<NavigateDesign> OneToOnes { get; set; } = new();
        public List<NavigateDesign> ManyToManys { get; set; } = new();
        public List<NavigateDesign> OneToManys { get; set; } = new();
        public KeyValuePair<string, TableRef> TreeNav { get; set; }
        public List<ColumnDesign> ColumnDesigns { get; set; } = new();
    }
    class ColumnDesign
    {
        public ColumnInfo Column { get; set; }
        public int Position { get; set; }
        public string DisplayText { get; set; }
        public bool IsDisplay { get; set; }
        public bool IsDisplayManyToOne { get; set; }
        public bool CanSearchText { get; set; }
        public string CanSearchTextTips { get; set; }
        public bool IsSearchText { get; set; }
        public bool IsSearchFilterEnum { get; set; }
        public bool IsSearchFilterManyToOne { get; set; }
        public bool CanEdit { get; set; }
        public int EditCol { get; set; }
        public int EditStyle { get; set; }
        public KeyValuePair<string, TableRef> ManyToOne { get; set; }
    }
    class NavigateDesign
    {
        public KeyValuePair<string, TableRef> Navigate { get; set; }
        public string DisplayText { get; set; }
        public bool IsDisplay { get; set; }
        public bool IsSearchFilter { get; set; }
        public bool IsSearchText { get; set; }
        public int EditStyle { get; set; }

        public List<ColumnDesign> RefColumnDesigns { get; set; }
    }

    Task CheckBoxChanged(ColumnDesign cd, int target, bool value)
    {
        if (value)
        {
            cd.IsDisplay = target == 0;
            cd.IsDisplayManyToOne = target == 1;
        }
        return Task.CompletedTask;
    }

    async Task BuildBlazorCode()
    {
        if (design.TableDescrptor == null)
        {
            await JS.Warning("请选择实体类型！");
            return;
        }
        var basePath = AppContext.BaseDirectory.Replace("\\", "/").TrimEnd('/');
        var bin_debug = basePath.IndexOf("/bin/Debug/");
        if (bin_debug != -1) basePath = basePath.Substring(0, bin_debug);
        var menuPath = Menu.Path.Trim('/');
        if (menuPath.EndsWith($"/{GetClassName(design.TableDescrptor.Type)}"))
        {
            var lastIndex = menuPath.LastIndexOf("/");
            menuPath = menuPath.Substring(0, menuPath.LastIndexOf("/")) + "/_" + menuPath.Substring(lastIndex + 1);
        }
        var savePath = basePath + "/Components/" + menuPath + ".razor";
        var savePaths = savePath.Split('/');
        savePaths[savePaths.Length - 1] = savePaths[savePaths.Length - 1][0].ToString().ToUpper() + savePaths[savePaths.Length - 1].Substring(1);
        savePath = string.Join("/", savePaths);
        // if (File.Exists(savePath))
        // {
        //     await JS.Error("文件已存在！", savePath);
        //     return;
        // }
        if (await JS.Confirm($"即将保存文件，确认继续吗？", savePath) == false) return;

        var alltables = new List<GenTableInfo>();
        alltables.Add(GetGenTableInfo(design.TableDescrptor));
        var genOptions = new GenRazorOptions
        {
            curd_PageSize = curd_PageSize,
            curd_IsRemove = curd_IsRemove,
            curd_IsAdd = curd_IsAdd,
            curd_IsEdit = curd_IsEdit,
            curd_IsMultiSelect = curd_IsMultiSelect,
            curd_FormInLine = curd_FormInLine,
            curd_DialogClassName = curd_DialogClassName,

            MenuPath = Menu.Path,
            MenuLabel = Menu.Label,

            Design = new()
            {
                IsTreeNav = !design.TreeNav.Key.IsNull(),
                ColumnDesigns = design.ColumnDesigns.Select(a => GetGenColumnDesign(a, alltables[0])).ToList(),
                OneToOnes = design.OneToOnes.Select(a => GetGenNavigateDesign(a, alltables[0], GenNavigateType.OneToOne)).ToList(),
                OneToManys = design.OneToManys.Select(a => GetGenNavigateDesign(a, alltables[0], GenNavigateType.OneToMany)).ToList(),
                ManyToManys = design.ManyToManys.Select(a => GetGenNavigateDesign(a, alltables[0], GenNavigateType.ManyToMany)).ToList(),
            },
            AllTables = alltables
        };

        var code = "";
        using (var client = new HttpClient())
        {
            client.BaseAddress = new Uri(AdminBlazorOptions.Global_GeneartorServer);
            client.DefaultRequestHeaders.Add("key", AdminBlazorOptions.Global_GeneartorKey);
            var content = new StringContent(JsonConvert.SerializeObject(genOptions), Encoding.UTF8, "application/json");
            var result = await client.PostAsync("GenBlazor", content);
            result.EnsureSuccessStatusCode();
            code = await result.Content.ReadAsStringAsync();
        }

        System.Console.WriteLine(code);
        var savePathDir = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(savePathDir)) Directory.CreateDirectory(savePathDir);
        File.WriteAllText(savePath, code);
        await JS.Success("文件已保存，请检查！", savePath);


        GenTableInfo GetOrAddGetGenTableInfo(Type entityType)
        {
            var fgentb = alltables.FirstOrDefault(a => a.TypeFullName == entityType.FullName);
            if (fgentb == null) alltables.Add(fgentb = GetGenTableInfo(fsql.CodeFirst.GetTableByEntity(entityType)));
            return fgentb;
        }
        GenNavigateDesign GetGenNavigateDesign(NavigateDesign nd, GenTableInfo gentb, GenNavigateType navtype)
        {
            var gentbref = GetOrAddGetGenTableInfo(nd.Navigate.Value.RefEntityType);
            return new GenNavigateDesign
            {
                Name = nd.Navigate.Key,

                RefType = navtype,
                RefTypeName = GetClassName(nd.Navigate.Value.RefEntityType),
                RefMiddleTypeName = navtype != GenNavigateType.ManyToMany ? null : GetClassName(nd.Navigate.Value.RefMiddleEntityType),

                Columns = gentb.Columns.Where(a => nd.Navigate.Value.Columns.Any(b => b.CsName == a.CsName)).ToList(),
                MiddleColumns = navtype != GenNavigateType.ManyToMany ? new() : GetOrAddGetGenTableInfo(nd.Navigate.Value.RefMiddleEntityType)
                        .Columns.Where(a => nd.Navigate.Value.Columns.Any(b => b.CsName == a.CsName)).ToList(),
                RefColumns = gentbref.Columns.Where(a => nd.Navigate.Value.Columns.Any(b => b.CsName == a.CsName)).ToList(),

                DisplayText = nd.DisplayText,
                IsDisplay = nd.IsDisplay,
                IsSearchFilter = nd.IsSearchFilter,
                IsSearchText = nd.IsSearchText,
                EditStyle = nd.EditStyle,

                RefColumnDesigns = nd.RefColumnDesigns?.Select(a => GetGenColumnDesign(a, gentbref)).ToList(),
            };
        }
        GenColumnDesign GetGenColumnDesign(ColumnDesign cd, GenTableInfo gentb)
        {
            return new GenColumnDesign
            {
                Column = gentb.Columns.FirstOrDefault(a => a.CsName == cd.Column.CsName),
                Position = cd.Position,
                DisplayText = cd.DisplayText,
                IsDisplay = cd.IsDisplay,
                IsDisplayManyToOne = cd.IsDisplayManyToOne,
                CanSearchText = cd.CanSearchText,
                CanSearchTextTips = cd.CanSearchTextTips,
                IsSearchText = cd.IsSearchText,
                IsSearchFilterEnum = cd.IsSearchFilterEnum,
                IsSearchFilterManyToOne = cd.IsSearchFilterManyToOne,
                CanEdit = cd.CanEdit,
                EditCol = cd.EditCol,
                EditStyle = cd.EditStyle,
                ManyToOne = cd.ManyToOne.Key.IsNull() ? null : new GenNavigateDesign
                {
                    Name = cd.ManyToOne.Key,

                    RefType = GenNavigateType.ManyToOne,
                    RefTypeName = GetClassName(cd.ManyToOne.Value.RefEntityType),

                    Columns = gentb.Columns.Where(a => cd.ManyToOne.Value.Columns.Any(b => b.CsName == a.CsName)).ToList(),
                    RefColumns = GetOrAddGetGenTableInfo(cd.ManyToOne.Value.RefEntityType).Columns.Where(a => cd.ManyToOne.Value.Columns.Any(b => b.CsName == a.CsName)).ToList(),
                },
            };
        }
    }
    GenTableInfo GetGenTableInfo(TableInfo tb)
    {
        var gentb = new GenTableInfo
        {
            TypeNameSpace = tb.Type.IsNested ? tb.Type.DeclaringType.Namespace : tb.Type.Namespace,
            TypeFullName = tb.Type.FullName,
            TypeName = GetClassName(tb.Type),
            CsName = tb.CsName,
            Comment = tb.Comment,
        };
        gentb.Columns.AddRange(tb.ColumnsByPosition.Select(a => new GenColumnInfo
        {
            TypeNameSpace = a.CsType.IsNested ? a.CsType.DeclaringType.Namespace : a.CsType.Namespace,
            TypeName = GetClassName(a.CsType),
            EnumValues = a.CsType.NullableTypeOrThis().IsEnum ?
                Enum.GetNames(a.CsType.NullableTypeOrThis()).Select(b =>
                    new KeyValuePair<string, long>(b, Enum.Parse(a.CsType.NullableTypeOrThis(), b).ConvertTo<long>())).ToDictionary(b => b.Key, b => b.Value) : new(),
            MapTypeName = GetClassName(a.Attribute.MapType),
            CsName = a.CsName,
            Comment = a.Comment,
            IsPrimary = a.Attribute.IsPrimary,
            IsIdentity = a.Attribute.IsIdentity,
            IsVersion = a.Attribute.IsVersion,
            DbType = a.Attribute.DbType,
            StringLength = a.Attribute.StringLength,
            Precision = a.Attribute.Precision,
            Scale = a.Attribute.Scale,
        }));
        return gentb;
    }

    public static string GetClassName(Type that) => that.IsNested ? $"{that.DeclaringType.Name}.{that.Name}" : (that.IsEnum ? that.Name : GetGenericName(that));
    public static string GetGenericName(Type that)
    {
        var ret = that?.NullableTypeOrThis().Name;
        if (that == typeof(bool) || that == typeof(bool?)) ret = "bool";

        else if (that == typeof(int) || that == typeof(int?)) ret = "int";
        else if (that == typeof(long) || that == typeof(long?)) ret = "long";
        else if (that == typeof(short) || that == typeof(short?)) ret = "short";
        else if (that == typeof(sbyte) || that == typeof(sbyte?)) ret = "sbyte";

        else if (that == typeof(uint) || that == typeof(uint?)) ret = "uint";
        else if (that == typeof(ulong) || that == typeof(ulong?)) ret = "ulong";
        else if (that == typeof(ushort) || that == typeof(ushort?)) ret = "ushort";
        else if (that == typeof(byte) || that == typeof(byte?)) ret = "byte";

        else if (that == typeof(double) || that == typeof(double?)) ret = "double";
        else if (that == typeof(float) || that == typeof(float?)) ret = "float";
        else if (that == typeof(decimal) || that == typeof(decimal?)) ret = "decimal";

        else if (that == typeof(string)) ret = "string";

        if (that.NullableTypeOrThis().IsEnum) return "enum";
        return (that.NullableTypeOrThis().IsEnum ? "enum " : "") + ret + (that.IsNullableType() ? "?" : "");
    }
}
