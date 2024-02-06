using FreeSql;
using Microsoft.Extensions.DependencyInjection;
using Rougamo.Context;
using System.Data;

[AttributeUsage(AttributeTargets.Method)]
public class TransactionalAttribute : Rougamo.MoAttribute
{
	public Propagation Propagation { get; set; } = Propagation.Required;
	public IsolationLevel IsolationLevel { get => m_IsolationLevel.Value; set => m_IsolationLevel = value; }
	IsolationLevel? m_IsolationLevel;

	public TransactionalAttribute(Propagation propagation)
	{
		Propagation = propagation;
	}
	public TransactionalAttribute(Propagation propagation, IsolationLevel isolationLevel)
	{
		Propagation = propagation;
		m_IsolationLevel = isolationLevel;
	}

	IUnitOfWork _uow;
	public override void OnEntry(MethodContext context)
	{
        var targetType = context.Target.GetType();
        var service = targetType.GetPropertyOrFieldValue(context.Target, "ServiceProvider") as IServiceProvider;
        if (service == null) throw new Exception($"_Imports.razor 未使用 @inject IServiceProvider ServiceProvider");

        var uowManager = service.GetService<UnitOfWorkManager>();
		_uow = uowManager.Begin(this.Propagation, this.m_IsolationLevel);
	}
	public override void OnExit(MethodContext context)
	{
		if (typeof(Task).IsAssignableFrom(context.RealReturnType))
			((Task)context.ReturnValue).ContinueWith(t => _OnExit());
		else _OnExit();

		void _OnExit()
		{
			try
			{
				if (context.Exception == null) _uow.Commit();
				else _uow.Rollback();
			}
			finally
			{
				_uow.Dispose();
			}
		}
	}
}