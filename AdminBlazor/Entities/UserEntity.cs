using FreeSql.DataAnnotations;

/// <summary>
/// 用户
/// </summary>
public partial class UserEntity : EntityCreated
{
    /// <summary>
    /// 名称
    /// </summary>
    [Column(StringLength = 50)]
    public string Username { get; set; }
    /// <summary>
    /// 昵称
    /// </summary>
    [Column(StringLength = 50)]
    public string Nickname { get; set; }
    /// <summary>
    /// 密码
    /// </summary>
    [Column(StringLength = 50)]
    public string Password { get; set; }

    /// <summary>
    /// 登陆时间
    /// </summary>
    public DateTime LoginTime { get; set; }
}

namespace BootstrapBlazor.Components
{
    /// <summary>
    /// 登陆日志
    /// </summary>
    public partial class UserLoginEntity : Entity
    {
        /// <summary>
        /// 登陆时间
        /// </summary>
        [Column(CanUpdate = false, ServerTime = DateTimeKind.Local)]
        public DateTime LoginTime { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [Column(StringLength = 50)]
        public string Username { get; set; }

        /// <summary>
        /// 日志类型
        /// </summary>
        public LoginType Type { get; set; }
        public enum LoginType { 登陆成功, 登陆失败 }

        /// <summary>
        /// IP
        /// </summary>
        [Column(StringLength = 50)]
        public string Ip { get; set; }
        /// <summary>
        /// 地点
        /// </summary>
        [Column(StringLength = 100)]
        public string City { get; set; }
        /// <summary>
        /// 浏览器
        /// </summary>
        public string Browser { get; set; }
        /// <summary>
        /// 操作系统
        /// </summary>
        [Column(StringLength = 50)]
        public string? OS { get; set; }
        /// <summary>
        /// 设备
        /// </summary>
        public WebClientDeviceType Device { get; set; }
        /// <summary>
        /// 浏览器语言
        /// </summary>
        [Column(StringLength = 50)]
        public string Language { get; set; }
        /// <summary>
        /// UserAgent
        /// </summary>
        public string? UserAgent { get; set; }
        /// <summary>
        /// 浏览器引擎信息
        /// </summary>
        public string? Engine { get; set; }
    }
}