using FreeSql.DataAnnotations;

namespace LinCms.Entities.Blog
{
    /// <summary>
    /// 评论信息
    /// </summary>
    [Table(Name = "blog_comment")]
    public class Comment : EntityModified
    {
        [Navigate(nameof(UserLike.SubjectId))]
        public List<UserLike> UserLikes { get; set; }

        /// <summary>
        /// 关联随笔
        /// </summary>
        public long ArticleId { get; set; }
        public Article Article { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        [Column(StringLength = 500)]
        public string Text { get; set; }

        /// <summary>
        /// 点赞量
        /// </summary>
        public int LikesQuantity { get; set; }

        /// <summary>
        /// 是否已审核
        /// </summary>
        public bool IsAudit { get; set; } = true;

    }
}