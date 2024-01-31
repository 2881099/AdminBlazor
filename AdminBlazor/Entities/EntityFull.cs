
/// <summary>
/// 实体基类
/// </summary>
public abstract class EntityFull<TKey> : EntitySoftDelete<TKey> where TKey : struct
{
}

/// <summary>
/// 实体基类
/// </summary>
public abstract class EntityFull : EntityFull<long>
{
}