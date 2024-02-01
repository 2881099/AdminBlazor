using FreeSql.DataAnnotations;

namespace LinCms.Entities.Blog
{
    /// <summary>
    /// 用户点赞
    /// </summary>
    [Table(Name = "blog_user_like")]
    public class UserLike : EntityCreated
    {
        /// <summary>
        /// 点赞对象
        /// </summary>
        public long SubjectId { get; set; }

        [Navigate(nameof(SubjectId))] public Comment Comment { get; set; }
        [Navigate(nameof(SubjectId))] public Article Article { get; set; }

        /// <summary>
        /// 点赞类型
        /// </summary>
        [Column(MapType = typeof(int))]
        public UserLikeSubjectType SubjectType { get; set; }

    }

    public enum UserLikeSubjectType
    {
        点赞随笔 = 0,
        点赞评论 = 1
    }
}
