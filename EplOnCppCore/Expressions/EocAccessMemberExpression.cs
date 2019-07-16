﻿using QIQI.EProjectFile.Expressions;

namespace QIQI.EplOnCpp.Core.Expressions
{
    public class EocAccessMemberExpression : EocExpression
    {
        public static EocAccessMemberExpression Translate(CodeConverter C, AccessMemberExpression expr)
        {
            if (expr == null) return null;
            return new EocAccessMemberExpression(C, EocExpression.Translate(C, expr.Target), C.P.GetEocMemberInfo(expr));
        }

        public EocAccessMemberExpression(CodeConverter c, EocExpression target, EocMemberInfo memberInfo) : base(c)
        {
            Target = target;
            MemberInfo = memberInfo;
        }

        public EocExpression Target { get; }
        public EocMemberInfo MemberInfo { get; }

        public override CppTypeName GetResultType()
        {
            return MemberInfo.DataType;
        }

        public override void WriteTo(CodeWriter writer)
        {
            if (!MemberInfo.Referencable)
            {
                writer.Write("e::system::noRef(");
            }
            if (MemberInfo.Getter != null)
            {
                writer.Write(MemberInfo.Getter);
                writer.Write("()");
            }
            else
            {
                writer.Write(MemberInfo.CppName);
            }
            if (!MemberInfo.Referencable)
            {
                writer.Write(")");
            }
        }
    }
}