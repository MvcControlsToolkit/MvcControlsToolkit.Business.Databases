using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MvcControlsToolkit.Business.DocumentDB.Internal
{
    internal class DependencyTracker
    {
        private Dictionary<PropertyInfo, DependencyTracker>
            registry = new Dictionary<PropertyInfo, DependencyTracker>();
        public DependencyTracker()
        {

        }
        public DependencyTracker(Stack<PropertyInfo> properties)
        {
            Add(properties);
        }
        public void Add(Stack<PropertyInfo> properties)
        {
            DependencyTracker res;
            var curr = properties.Pop();
            
            if (registry.TryGetValue(curr, out res))
            {
                if (properties.Count != 0)
                {
                    if (res == null)
                    {
                        registry[curr] = new DependencyTracker(properties);
                    }
                    else res.Add(properties);
                }
            }
            else registry.Add(curr, properties.Count == 0 ? null : new DependencyTracker(properties));

        }
        public LambdaExpression GetExpression(Type type)
        {
            var par = Expression.Parameter(type);
            return Expression.Lambda( GetExpression(par, type), par);
        }
        private Expression GetExpression(Expression par, Type type)
        {
            var assignements = new List<MemberBinding>();
            foreach(var pair in registry )
            {
                MemberBinding binding;
                if (pair.Value == null)
                {
                    binding = Expression.Bind(pair.Key,
                        Expression.Property(par, pair.Key));
                }
                else
                {

                    binding = Expression.Bind(pair.Key,
                        pair.Value.GetExpression(Expression.Property(par, pair.Key), pair.Key.PropertyType));
                }
                assignements.Add(binding);

            }
            return Expression.MemberInit(Expression.New(type), assignements);
        }
    }
}
