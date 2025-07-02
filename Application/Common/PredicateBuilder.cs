using System.Linq.Expressions;

namespace Application.Common;

/// <summary>
/// Provides utility methods for dynamically building predicates for LINQ queries
/// </summary>
public static class PredicateBuilder
{
    /// <summary>
    /// Creates a predicate that evaluates to true
    /// </summary>
    public static Expression<Func<T, bool>> True<T>() => param => true;

    /// <summary>
    /// Creates a predicate that evaluates to false
    /// </summary>
    public static Expression<Func<T, bool>> False<T>() => param => false;

    /// <summary>
    /// Creates a predicate with the given expression
    /// </summary>
    public static Expression<Func<T, bool>> Create<T>(Expression<Func<T, bool>> predicate) => predicate;

    /// <summary>
    /// Combines two predicates with logical AND
    /// </summary>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
    {
        var parameter = Expression.Parameter(typeof(T), "param");
        var body = Expression.AndAlso(
            ReplaceParameter(first.Body, first.Parameters[0], parameter),
            ReplaceParameter(second.Body, second.Parameters[0], parameter)
        );
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    /// <summary>
    /// Combines two predicates with logical OR
    /// </summary>
    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
    {
        var parameter = Expression.Parameter(typeof(T), "param");
        var body = Expression.OrElse(
            ReplaceParameter(first.Body, first.Parameters[0], parameter),
            ReplaceParameter(second.Body, second.Parameters[0], parameter)
        );
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    /// <summary>
    /// Negates the given predicate with logical NOT
    /// </summary>
    public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expression)
    {
        var parameter = Expression.Parameter(typeof(T), "param");
        var body = Expression.Not(ReplaceParameter(expression.Body, expression.Parameters[0], parameter));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
    }

    private sealed class ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter) : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter = oldParameter;
        private readonly ParameterExpression _newParameter = newParameter;

		protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}