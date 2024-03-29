﻿using FreeSql.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public interface IEntityCreated
{
    /// <summary>
    /// 创建者用户Id
    /// </summary>
    long? CreatedUserId { get; set; }
    /// <summary>
    /// 创建者
    /// </summary>
    string CreatedUserName { get; set; }
    /// <summary>
    /// 创建时间
    /// </summary>
    DateTime? CreatedTime { get; set; }
}

/// <summary>
/// 实体创建
/// </summary>
public abstract class EntityCreated<TKey> : Entity<TKey>, IEntityCreated
{
    /// <summary>
    /// 创建者Id
    /// </summary>
    [Column(Position = -22, CanUpdate = false)]
    public virtual long? CreatedUserId { get; set; }

    /// <summary>
    /// 创建者
    /// </summary>
    [Column(Position = -21, CanUpdate = false), MaxLength(50)]
    public virtual string CreatedUserName { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Column(Position = -20, CanUpdate = false, ServerTime = DateTimeKind.Local)]
    public virtual DateTime? CreatedTime { get; set; }
}

/// <summary>
/// 实体创建
/// </summary>
public abstract class EntityCreated : EntityCreated<long>
{
}