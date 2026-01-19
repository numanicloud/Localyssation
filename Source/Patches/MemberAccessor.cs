using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Localyssation.Patches
{
    public static class MemberAccessor<TThis>
    {
        public static FieldInfo GetFieldInfo(Expression<Func<TThis, object>> expr)
        {
            // x => x.field
            if (expr.Body is MemberExpression member)
            {
                return (FieldInfo)member.Member;
            }

            // x => (object)x.field のような場合（ボクシング）
            if (expr.Body is UnaryExpression unary &&
                unary.Operand is MemberExpression unaryMember)
            {
                return (FieldInfo)unaryMember.Member;
            }

            throw new ArgumentException(
                $"Expression {expr} is not a member access expression.",
                nameof(expr));
        }
    }
}
