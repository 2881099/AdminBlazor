using FreeSql;
using FreeSql.DataAnnotations;

namespace BootstrapBlazor.Components
{
    /// <summary>
    /// 租户
    /// </summary>
    public partial class TenantEntity : EntityCreated<string>
    {
        /// <summary>
        /// 数据库模板
        /// </summary>
        public long DatabaseId { get; set; }
        public TenantDatabaseEntity Database { get; set; }

        /// <summary>
        /// 域名
        /// </summary>
        [Column(StringLength = 50)]
        public string Host { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 说明
        /// </summary>
        [Column(StringLength = 500)] 
        public string Description { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

        [Navigate(ManyToMany = typeof(TenantMenuEntity))]
        public List<MenuEntity> Menus { get; set; }
    }
    partial class MenuEntity
    {
        [Navigate(ManyToMany = typeof(TenantMenuEntity))]
        public List<TenantEntity> Tenants { get; set; }
    }
    public class TenantMenuEntity
    {
        public string TenantId { get; set; }
        public long MenuId { get; set; }

        public TenantEntity Tenant { get; set; }
        public MenuEntity Menu { get; set; }
    }

    /// <summary>
    /// 数据库
    /// </summary>
    public partial class TenantDatabaseEntity : EntityModified
    {
        /// <summary>
        /// 显示名
        /// </summary>
        [Column(StringLength = 50)]
        public string Label { get; set; }
        /// <summary>
        /// 类型
        /// </summary>
        public DataType DataType { get; set; }
        /// <summary>
        /// 连接串
        /// </summary>
        [Column(StringLength = 500)]
        public string ConenctionString { get; set; }
    }
}