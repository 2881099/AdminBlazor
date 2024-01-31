using FreeSql.DataAnnotations;
using System.ComponentModel;

public interface IEntity<TKey>
{
    /// <summary>
    /// 主键Id
    /// </summary>
    TKey Id { get; set; }
}

public abstract class Entity<TKey> : IEntity<TKey>
{
    /// <summary>
    /// 主键Id
    /// </summary>
    [Description("主键Id")]
    [Snowflake]
    [Column(Position = 1, IsIdentity = false, IsPrimary = true)]
    public virtual TKey Id { get; set; }
}

public abstract class Entity : Entity<long>
{
}