using System.Reflection;
using Cronos;

namespace GZCTF.Services.CronJob;

[AttributeUsage(AttributeTargets.Method)]
public class CronJobAttribute(string expression) : Attribute
{
    public CronExpression Expression { get; } = CronExpression.Parse(expression);
}

public class CronJobNotFoundException(string message) : Exception(message);

public static class CronJobExtensions
{
    extension(CronJob job)
    {
        public (string, CronJobEntry) ToEntry()
        {
            var method = job.Method;
            var attr = method.GetCustomAttribute<CronJobAttribute>() ??
                       throw new CronJobNotFoundException(method.Name);
            return (method.Name, new CronJobEntry(job, attr.Expression));
        }
    }
}
