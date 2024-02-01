using FreeSql.DataAnnotations;

namespace LinCms.Entities.Blog
{
    partial class Tag2
    {
        [Navigate(ManyToMany = typeof(TagArticle))]
        public List<Article> Articles { get; set; }
        [Table(Name = "blog_tag_article")]
        public class TagArticle
        {
            public long TagId { get; set; }
            public long ArticleId { get; set; }

            public Tag2 Tag { get; set; }
            public Article Article { get; set; }
        }
    }

    /// <summary>
    /// 随笔
    /// </summary>
    [Table(Name = "blog_article")]
    public partial class Article : EntityModified
    {
        [Navigate(ManyToMany = typeof(Tag2.TagArticle))]
        public List<Tag2> Tags { get; set; }

        [Navigate(nameof(UserLike.SubjectId))]
        public List<UserLike> UserLikes { get; set; }

        /// <summary>
        /// 随笔专栏
        /// </summary>
        public long? ClassifyId { get; set; }
        public Classify Classify { get; set; }

        /// <summary>
        /// 技术频道
        /// </summary>
        public long ChannelId { get; set; }
        public Channel Channel { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        [Column(StringLength = 200)]
        public string Title { get; set; }

        /// <summary>
        /// 关键字
        /// </summary>
        [Column(StringLength = 400)]
        public string Keywords { get; set; }

        /// <summary>
        /// 来源
        /// </summary>
        [Column(StringLength = 400)]
        public string Source { get; set; }

        /// <summary>
        /// 摘要
        /// </summary>
        [Column(StringLength = 500)]
        public string Excerpt { get; set; }

        /// <summary>
        /// 正文
        /// </summary>
        [Column(StringLength = -2)]
        public string Content { get; set; }

        /// <summary>
        /// 浏览量
        /// </summary>
        public int ViewHits { get; set; }

        /// <summary>
        /// 评论数量
        /// </summary>
        public int CommentQuantity { get; set; }

        /// <summary>
        /// 点赞数量
        /// </summary>
        public int LikesQuantity { get; set; }
        
        /// <summary>
        /// 收藏数量
        /// </summary>
        public int CollectQuantity { get; set; }

        /// <summary>
        /// 缩略图
        /// </summary>
        [Column(StringLength = 400)]
        public string Thumbnail { get; set; }

        /// <summary>
        /// 是否审核
        /// </summary>
        public bool IsAudit { get; set; }

        /// <summary>
        /// 是否推荐
        /// </summary>
        public bool Recommend { get; set; }

        /// <summary>
        /// 是否置顶
        /// </summary>
        public bool IsStickie { get; set; }

        /// <summary>
        /// 随笔类型
        /// </summary>
        [Column(MapType = typeof(int))]
        public ArticleType ArticleType { get; set; }

        /// <summary>
        /// 字数
        /// </summary>
        public long WordNumber { get; set; }

        /// <summary>
        /// 阅读时长
        /// </summary>
        public long ReadingTime { get; set; }

        /// <summary>
        /// 开启评论
        /// </summary>
        public bool Commentable { get; set; } = true;
    }

    /// <summary>
    /// 随笔类型
    /// </summary>
    public enum ArticleType
    {
        原创,
        转载,
        翻译,
    }
}
