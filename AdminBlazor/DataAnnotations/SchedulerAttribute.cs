
using FreeScheduler;

[AttributeUsage(AttributeTargets.Method)]
public class SchedulerAttribute : Attribute
{
	public string Name { get; set; }
	public TaskInterval Interval { get; set; }
	public string Argument { get; set; }
	public int Round { get; set; } = -1;
    public FreeScheduler.TaskStatus Status { get; set; }

	public SchedulerAttribute(string name)
	{
		this.Name = name;
    }
    public SchedulerAttribute(string name, string cron)
    {
        this.Name = name;
		this.Interval = TaskInterval.Custom;
		this.Argument = cron;
    }
}