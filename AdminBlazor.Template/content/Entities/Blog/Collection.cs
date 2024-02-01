using FreeSql.DataAnnotations;

namespace LinCms.Entities.Blog;

partial class Article
{
    public List<Collection> Collections { get; set; }
    /// <summary>
    /// 文章收藏
    /// </summary>
    [Table(Name = "blog_article_collection")]
    public class ArticleCollection : IEntityCreated
    {
        [Column(IsPrimary = true)]
        public long ArticleId { get; set; }
        [Column(IsPrimary = true)]
        public long CollectionId { get; set; }
        public Article Article { get; set; }
        public Collection Collection { get; set; }

        public long? CreatedUserId { get; set; }
        public string CreatedUserName { get; set; }
        public DateTime? CreatedTime { get; set; }
    }
}
/// <summary>
/// 收藏集
/// </summary>
[Table(Name = "blog_collection")]
public class Collection : EntityModified
{
    public List<Article> Articles { get; set; }

    /// <summary>
    /// 名称 
    /// </summary>
    [Column(StringLength = 50)]
    public string Name { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [Column(StringLength = 500)]
    public string Remark { get; set; }

    /// <summary>
    /// 收藏数量 
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 类型
    /// </summary>
    public PrivacyType PrivacyType { get; set; }
}

public enum PrivacyType
{
    公开可见 = 0,
    仅自己可见 = 1
}