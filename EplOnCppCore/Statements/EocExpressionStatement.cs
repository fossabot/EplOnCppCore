﻿using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EProjectFile.Statements;
using QIQI.EProjectFile;

namespace QIQI.EplOnCpp.Core.Statements
{
    public class EocExpressionStatement : EocStatement
    {
        public static EocExpressionStatement Translate(CodeConverter C, ExpressionStatement stat)
        {
            if (stat == null) return null;
            if (stat.Mask)
            {
                return new EocExpressionStatement(
                    C,
                    null,
                    stat.Expression.ToTextCode(C.P.IdToNameMap) + " '" + stat.Comment);
            }
            else
            {
                return new EocExpressionStatement(
                    C,
                    EocExpression.Translate(C, stat.Expression),
                    stat.Comment);
            }
        }

        public EocExpression Expr { get; set; }
        public string Comment { get; set; }

        public EocExpressionStatement(CodeConverter c, EocExpression expr, string comment) : base(c)
        {
            Expr = expr;
            Comment = comment;
        }

        public override void WriteTo()
        {
            Writer.NewLine();
            if (Expr != null)
            {
                Expr.WriteTo();
                Writer.Write("; ");
            }
            Writer.AddComment(Comment);
        }
    }
}