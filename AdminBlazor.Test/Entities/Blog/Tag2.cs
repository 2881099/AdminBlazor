using FreeSql.DataAnnotations;

namespace LinCms.Entities.Blog
{
    /// <summary>
    /// 标签
    /// </summary>
    [Table(Name = "blog_tag")]
    public partial class Tag2 : EntityCreated
    {
        /// <summary>
        /// 标签名
        /// </summary>
        [Column(StringLength = 50)]
        public string TagName { get; set; }

        /// <summary>
        /// 别名
        /// </summary>
        [Column(StringLength = 200)]
        public string Alias { get; set; }

        /// <summary>
        /// 封面图
        /// </summary>
        [Column(StringLength = 100)]
        public string Thumbnail { get; set; }

        /// <summary>
        /// 标签状态
        /// </summary>
        public bool Status { get; set; }

        /// <summary>
        /// 随笔数量
        /// </summary>
        public int ArticleCount { get; set; } = 0;

        /// <summary>
        /// 浏览次数
        /// </summary>
        public int ViewHits { get; set; } = 0;

        /// <summary>
        /// 备注
        /// </summary>
        [Column(StringLength = 500)]
        public string Remark { get; set; }
    }
}
