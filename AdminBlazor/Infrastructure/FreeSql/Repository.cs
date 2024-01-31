using FreeSql;

class BasicRepository<TEntity, TKey> : BaseRepository<TEntity, TKey> where TEntity : class
{
    public BasicRepository(IFreeSql fsql) : base(fsql, null, null) { }
    public BasicRepository(IFreeSql fsql, UnitOfWorkManager uowManager) : base(uowManager?.Orm ?? fsql, null, null)
    {
        uowManager?.Binding(this);
    }
    public BasicRepository(IFreeSql fsql, UnitOfWorkManager uowManager, RepositoryOptions options) : base(uowManager?.Orm ?? fsql, null, null)
    {
        uowManager?.Binding(this);
        if (options != null)
        {
            DbContextOptions.NoneParameter = options.NoneParameter;
            DbContextOptions.EnableGlobalFilter = options.EnableGlobalFilter;
            DbContextOptions.AuditValue = options.AuditValue;
        }
    }
}
class BasicRepository<TEntity> : BasicRepository<TEntity, long> where TEntity : class
{
    public BasicRepository(IFreeSql fsql) : base(fsql) { }
    public BasicRepository(IFreeSql fsql, UnitOfWorkManager uowManager) : base(fsql, uowManager) { }
    public BasicRepository(IFreeSql fsql, UnitOfWorkManager uowManager, RepositoryOptions options) : base(fsql, uowManager, options) { }
}

class DddRepository<TEntity> : AggregateRootRepository<TEntity> where TEntity : class
{
    public DddRepository(IFreeSql fsql) : base(fsql) { }
    public DddRepository(IFreeSql fsql, UnitOfWorkManager uowManager) : base(fsql, uowManager) { }
    public DddRepository(IFreeSql fsql, UnitOfWorkManager uowManager, RepositoryOptions options) : base(fsql, uowManager)
    {
        if (options != null)
        {
            DbContextOptions.NoneParameter = options.NoneParameter;
            DbContextOptions.EnableGlobalFilter = options.EnableGlobalFilter;
            DbContextOptions.AuditValue = options.AuditValue;
        }
    }

    public override ISelect<TEntity> Select => base.SelectDiy;
}

class RepositoryOptions
{
    /// <summary>
    /// 使用无参数化设置（对应 IInsert/IUpdate）
    /// </summary>
    public bool? NoneParameter { get; set; }

    /// <summary>
    /// 是否开启 IFreeSql GlobalFilter 功能（默认：true）
    /// </summary>
    public bool EnableGlobalFilter { get; set; } = true;

    /// <summary>
    /// DbContext/Repository 审计值事件，适合 Scoped IOC 中获取登陆信息
    /// </summary>
    public Action<DbContextAuditValueEventArgs> AuditValue { get; set; }
}