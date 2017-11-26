using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MvcControlsToolkit.Business.DocumentDB.Internal
{
    internal class SortincClauseInfos
    {
        public bool Asc { get; set; }
        public Expression Path { get; set; }
    }
    internal class SortingConversionQueryable<T> : IQueryable<T>, IQueryProvider, IOrderedQueryable<T>
    {
        private const string orderby = "OrderBy";
        private const string thenby = "ThenBy";
        public Expression Expression { get; set; }

        public Type ElementType { get; set; }

        public IQueryProvider Provider
        {
            get { return this; }
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new SortingConversionQueryable<T>
            {
                ElementType = expression.Type,
                Expression=expression
            };
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new SortingConversionQueryable<TElement>
            {
                ElementType = typeof(TElement),
                Expression = expression
            };
        }

        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        public TResult Execute<TResult>(Expression expression)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        

        public List<SortincClauseInfos> GetSorting()
        {
            var res = new List<SortincClauseInfos>();
            var curr = Expression;
            while (curr.NodeType == ExpressionType.Call)
            {
                var call = curr as MethodCallExpression;
                res.Add(new SortincClauseInfos
                    {
                        Path =call.Arguments[1] as Expression,
                        Asc =call.Method.Name == orderby ||  call.Method.Name == thenby
                }
                );
                curr = call.Arguments[0];
            }
            res.Reverse();
            return res;
        }
    }
}
