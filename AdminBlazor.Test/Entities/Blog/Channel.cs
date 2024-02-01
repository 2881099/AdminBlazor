using FreeSql.DataAnnotations;

namespace LinCms.Entities.Blog
{
    partial class Tag2
    {
        [Navigate(ManyToMany = typeof(ChannelTag2))]
        public List<Channel> Channels { get; set; }
        [Table(Name = "blog_channel_tag")]
        public class ChannelTag2
        {
            public long ChannelId { get; set; }
            public long TagId { get; set; }

            public Channel Channel { get; set; }
            public Tag2 Tag { get; set; }
        }
    }

    /// <summary>
    /// 技术频道
    /// </summary>
    [Table(Name = "blog_channel")]
    public class Channel : EntityModified
    {
        [Navigate(ManyToMany = typeof(Tag2.ChannelTag2))]
        public List<Tag2> Tags { get; set; }

        /// <summary>
        /// 频道名称
        /// </summary>
        [Column(StringLength = 50)]
        public string ChannelName { get; set; }

        /// <summary>
        /// 编码
        /// </summary>
        [Column(StringLength = 50)]
        public string ChannelCode { get; set; }


        /// <summary>
        /// 封面图
        /// </summary>
        [Column(StringLength = 100)]
        public string Thumbnail { get; set; }

        /// <summary>
        /// 备注描述
        /// </summary>
        [Column(StringLength = 500)]
        public string Remark { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int SortCode { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool Status { get; set; }
    }
}
