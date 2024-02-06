using FreeSql.DataAnnotations;

namespace BootstrapBlazor.Components
{
    /// <summary>
    /// 数据字典
    /// </summary>
    public partial class DictEntity : Entity<long>
    {
        /// <summary>
        /// 字典类型
        /// </summary>
        public long ParentId { get; set; }

        /// <summary>
        /// 编码
        /// </summary>
        [Column(StringLength = 50)]
        public string Name { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        [Column(StringLength = 50)]
        public string Value { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        [Column(StringLength = 500)]
        public string Description { get; set; }

        /// <summary>
        /// 启用
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 排序
        /// </summary>
        public int Sort { get; set; }
    }
}