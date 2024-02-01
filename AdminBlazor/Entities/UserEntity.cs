using FreeSql.DataAnnotations;


/// <summary>
/// 用户
/// </summary>
public partial class UserEntity : EntityCreated
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Username { get; set; }
    /// <summary>
    /// 昵称
    /// </summary>
    public string Nickname { get; set; }
    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// 登陆时间
    /// </summary>
    public DateTime LoginTime { get; set; }

    [Navigate(nameof(Id))]
    public UserExtEntity UserExt { get; set; }
}

/// <summary>
/// 用户扩展
/// </summary>
public partial class UserExtEntity
{
    [Column(IsPrimary = true)]
    public long UserId { get; set; }
    [Navigate(nameof(UserId))]
    public UserEntity User { get; set; }
    /// <summary>
    /// 年龄
    /// </summary>
    public int Age { get; set; }
    /// <summary>
    /// 籍贯
    /// </summary>
    public string Whois { get; set; }
}
