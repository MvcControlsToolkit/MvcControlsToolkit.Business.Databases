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
    internal static class ExpressionExtensions
    {

        public static Expression CloneExpression(this Expression expression, bool removeIEnumerables = false, DependencyTracker tracker=null)
        {
            var lambda=expression as LambdaExpression;
            return CloneExpression(expression,lambda?.Parameters[0], null, removeIEnumerables, tracker);
        }
        private static TypeInfo lambda = typeof(LambdaExpression).GetTypeInfo();
        private static TypeInfo methodCall = typeof(MethodCallExpression).GetTypeInfo();
        private static TypeInfo memberExpression = typeof(MemberExpression).GetTypeInfo();
        private static TypeInfo unaryExpression = typeof(UnaryExpression).GetTypeInfo();
        private static TypeInfo binaryExpression = typeof(BinaryExpression).GetTypeInfo();
        private static TypeInfo parameterExpression = typeof(ParameterExpression).GetTypeInfo();
        private static TypeInfo conditionalExpression = typeof(ConditionalExpression).GetTypeInfo();
        private static TypeInfo memberInitExpression = typeof(MemberInitExpression).GetTypeInfo();
        private static TypeInfo newExpression = typeof(NewExpression).GetTypeInfo();
        private static TypeInfo constantExpression = typeof(ConstantExpression).GetTypeInfo();
        public static Expression CloneExpression(this Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter, bool removeIEnumerables=false, DependencyTracker tracker = null)
        {
            Expression exp = null;
            Type expressionType = expression.GetType();
            if (parameterExpression.IsAssignableFrom(expressionType))
            {
                exp = ((ParameterExpression)expression).CloneExpression(oldParameter, newParameter, removeIEnumerables, tracker);
            }
            else if (memberExpression.IsAssignableFrom(expressionType))
            {
                exp = ((MemberExpression)expression).CloneExpression(oldParameter, newParameter, removeIEnumerables, tracker);
            }
            else if (methodCall.IsAssignableFrom(expressionType))
            {
                exp = ((MethodCallExpression)expression).CloneExpression(oldParameter, newParameter, removeIEnumerables, tracker);
            }
            else if (newExpression.IsAssignableFrom(expressionType))
            {
                exp = ((NewExpression)expression).CloneExpression(oldParameter, newParameter, removeIEnumerables, tracker);
            }
            else if (unaryExpression.IsAssignableFrom(expressionType))
            {
                exp = ((UnaryExpression)expression).CloneExpression(oldParameter, newParameter, removeIEnumerables, tracker);
            }
            else if (constantExpression.IsAssignableFrom(expressionType))
            {
                exp = ((ConstantExpression)expression).CloneExpression(oldParameter, newParameter, removeIEnumerables, tracker);
            }
            else if (conditionalExpression.IsAssignableFrom(expressionType))
            {
                exp = ((ConditionalExpression)expression).CloneExpression(oldParameter, newParameter, removeIEnumerables, tracker);
            }
            else if (lambda.IsAssignableFrom(expressionType))
            {
                exp = ((LambdaExpression)expression).CloneExpression(oldParameter, newParameter, removeIEnumerables, tracker);
            }
            else if (memberInitExpression.IsAssignableFrom(expressionType))
            {
                exp = ((MemberInitExpression)expression).CloneExpression(oldParameter, newParameter, removeIEnumerables, tracker);
            }
            else if (binaryExpression.IsAssignableFrom(expressionType))
            {
                exp = ((BinaryExpression)expression).CloneExpression(oldParameter, newParameter, removeIEnumerables, tracker);
            }
            else
            {
                //did I forget some expression type? probably. this will take care of that... :)
                throw new NotImplementedException("Expression type " + expression.GetType().FullName + " not supported by this expression tree parser.");
            }
            return exp;
        }

        
        public static LambdaExpression CloneExpression(this LambdaExpression expression, ParameterExpression oldParameter, ParameterExpression newParameter, bool removeIEnumerable, DependencyTracker tracker)
        {
            LambdaExpression lambdaExpression = null;
            lambdaExpression = Expression.Lambda(
                expression.Type,
                expression.Body.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker),
                (expression.Parameters != null) ? expression.Parameters.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker) : null
                );
            return lambdaExpression;
        }

       
        public static BinaryExpression CloneExpression(this BinaryExpression expression, ParameterExpression oldParameter, ParameterExpression newParameter, bool removeIEnumerable, DependencyTracker tracker)
        {
            BinaryExpression binaryExp = null;
            binaryExp = Expression.MakeBinary(
                expression.NodeType,
                (expression.Left != null) ? expression.Left.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker) : null,
                (expression.Right != null) ? expression.Right.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker) : null,
                expression.IsLiftedToNull,
                expression.Method,
                (expression.Conversion != null) ? expression.Conversion.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker) : null
                );
            return binaryExp;
        }

        
        public static ParameterExpression CloneExpression(this ParameterExpression expression, ParameterExpression oldParameter, ParameterExpression newParameter, bool removeIEnumerable, DependencyTracker tracker)
        {
            ParameterExpression paramExpression = null;
            if (newParameter != null && expression.Equals(oldParameter))
            {
                paramExpression = newParameter;
            }
            else
            {
                paramExpression = expression;
            }
            return paramExpression;
        }

        
        public static MemberExpression CloneExpression(this MemberExpression expression, ParameterExpression oldParameter, ParameterExpression newParameter, bool removeIEnumerable, DependencyTracker tracker)
        {
            var newTracker = tracker;
            if(tracker != null )
            {
                PropertyInfo property = expression.Member as PropertyInfo;
                Expression currExpression = expression;
                var stack = new Stack<PropertyInfo>();
                if(property != null)
                {
                    newTracker = null;
                    while(property != null)
                    {
                        stack.Push(property);
                        currExpression = (currExpression as MemberExpression).Expression;
                        property = (currExpression as MemberExpression)?.Member as PropertyInfo;
                    }
                    if ((currExpression as ParameterExpression) == oldParameter) tracker.Add(stack);
                }
            }
            return Expression.MakeMemberAccess(
                (expression.Expression != null) ? expression.Expression.CloneExpression(oldParameter, newParameter, removeIEnumerable, newTracker) : null,
                expression.Member);
        }

        
        public static MemberInitExpression CloneExpression(this MemberInitExpression expression, ParameterExpression oldParameter, ParameterExpression newParameter, bool removeIEnumerable, DependencyTracker tracker)
        {
            return Expression.MemberInit(
                (expression.NewExpression != null) ? expression.NewExpression.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker) : null,
                (expression.Bindings != null) ? expression.Bindings.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker) : null
                );
        }

       
        public static MethodCallExpression CloneExpression(this MethodCallExpression expression, ParameterExpression oldParameter, ParameterExpression newParameter, bool removeIEnumerable, DependencyTracker tracker)
        {
            MethodCallExpression callExpression = null;
            callExpression = Expression.Call(
                (expression.Object != null) ? expression.Object.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker) : null,
                expression.Method,
                (expression.Arguments != null) ? expression.Arguments.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker) : null
                );
            return callExpression;
        }

        
        public static NewExpression CloneExpression(this NewExpression expression, ParameterExpression oldParameter, ParameterExpression newParameter, bool removeIEnumerable, DependencyTracker tracker)
        {
            return Expression.New(
                expression.Constructor,
                (expression.Arguments != null) ? expression.Arguments.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker) : null,
                expression.Members);
        }

        
        public static IEnumerable<ParameterExpression> CloneExpression(this System.Collections.ObjectModel.ReadOnlyCollection<ParameterExpression> expressionArguments, ParameterExpression oldParameter, ParameterExpression newParameter, bool removeIEnumerable, DependencyTracker tracker)
        {
            if (expressionArguments != null)
            {
                foreach (ParameterExpression argument in expressionArguments)
                {
                    if (argument != null)
                    {
                        yield return argument.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker);
                    }
                    else
                    {
                        yield return null;
                    }
                }
            }
        }

        
        public static IEnumerable<Expression> CloneExpression(this System.Collections.ObjectModel.ReadOnlyCollection<Expression> expressionArguments, ParameterExpression oldParameter, ParameterExpression newParameter, bool removeIEnumerable, DependencyTracker tracker)
        {
            if (expressionArguments != null)
            {
                foreach (Expression argument in expressionArguments)
                {
                    if (argument != null)
                    {
                        yield return argument.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker);
                    }
                    else
                    {
                        yield return null;
                    }
                }
            }
        }

        
        public static IEnumerable<ElementInit> CloneExpression(this System.Collections.ObjectModel.ReadOnlyCollection<ElementInit> elementInits, ParameterExpression oldParameter, ParameterExpression newParameter, bool removeIEnumerable, DependencyTracker tracker)
        {
            if (elementInits != null)
            {
                foreach (ElementInit elementInit in elementInits)
                {
                    if (elementInit != null)
                    {
                        yield return Expression.ElementInit(elementInit.AddMethod, elementInit.Arguments.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker));
                    }
                    else
                    {
                        yield return null;
                    }
                }
            }
        }

        
        public static IEnumerable<MemberBinding> CloneExpression(this System.Collections.ObjectModel.ReadOnlyCollection<MemberBinding> memberBindings, ParameterExpression oldParameter, ParameterExpression newParameter, bool removeIEnumerable, DependencyTracker tracker)
        {
            if (memberBindings != null)
            {
                foreach (MemberBinding binding in memberBindings)
                {
                    if (binding != null)
                    {
                        switch (binding.BindingType)
                        {
                            case MemberBindingType.Assignment:
                                MemberAssignment memberAssignment = (MemberAssignment)binding;
                                if (removeIEnumerable)
                                {
                                    PropertyInfo prop = memberAssignment.Member as PropertyInfo;
                                    if (prop != null && typeof(IEnumerable) != typeof(string) && typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                                    {
                                        if (tracker != null) memberAssignment.Expression.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker);
                                        continue;
                                    }
                                }
                                yield return Expression.Bind(binding.Member, memberAssignment.Expression.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker));
                                break;
                            case MemberBindingType.ListBinding:
                                MemberListBinding listBinding = (MemberListBinding)binding;
                                yield return Expression.ListBind(binding.Member, listBinding.Initializers.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker));
                                break;
                            case MemberBindingType.MemberBinding:
                                MemberMemberBinding memberMemberBinding = (MemberMemberBinding)binding;
                                yield return Expression.MemberBind(binding.Member, memberMemberBinding.Bindings.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker));
                                break;
                        }
                    }
                    else
                    {
                        yield return null;
                    }
                }
            }
        }


        public static UnaryExpression CloneExpression(this UnaryExpression expression, ParameterExpression oldParameter, ParameterExpression newParameter, bool removeIEnumerable, DependencyTracker tracker)
        {
            return Expression.MakeUnary(
                expression.NodeType,
                (expression.Operand != null) ? expression.Operand.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker) : null,
                expression.Type,
                expression.Method);
        }


        public static ConstantExpression CloneExpression(this ConstantExpression expression, ParameterExpression oldParameter, ParameterExpression newParameter, bool removeIEnumerable, DependencyTracker tracker)
        {
            //return Expression.Constant(expression.Value, expression.Type);
            return expression;
        }

        
        public static ConditionalExpression CloneExpression(this ConditionalExpression expression, ParameterExpression oldParameter, ParameterExpression newParameter, bool removeIEnumerable, DependencyTracker tracker)
        {
            return Expression.Condition(
                (expression.Test != null) ? expression.Test.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker) : null,
                (expression.IfTrue != null) ? expression.IfTrue.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker) : null,
                (expression.IfFalse != null) ? expression.IfFalse.CloneExpression(oldParameter, newParameter, removeIEnumerable, tracker) : null
                );
        }
    }
}
