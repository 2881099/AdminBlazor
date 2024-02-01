using FreeSql.DataAnnotations;

namespace LinCms.Entities.Blog
{

    /// <summary>
    /// 随笔专栏
    /// </summary>
    [Table(Name = "blog_classify")]
    public class Classify : EntityCreated
    {
        /// <summary>
        /// 分类专栏名称
        /// </summary>
        [Column(StringLength = 50)]
        public string ClassifyName { get; set; }
        /// <summary>
        /// 排序
        /// </summary>
        public int SortCode { get; set; }

        /// <summary>
        /// 随笔数量
        /// </summary>
        public int ArticleCount { get; set; }
        /// <summary>
        /// 封面图
        /// </summary>
        [Column(StringLength = 100)]
        public string Thumbnail { get; set; }
    }
}
