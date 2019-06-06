﻿using QIQI.EProjectFile;
using QIQI.EProjectFile.Expressions;
using System;

namespace QIQI.EplOnCpp.Core.Expressions
{
    public class EocVariableExpression : EocExpression
    {
        public static EocVariableExpression Translate(CodeConverter C, VariableExpression expr)
        {
            if (expr == null) return null;
            switch (EplSystemId.GetType(expr.Id))
            {
                case EplSystemId.Type_Local:
                    if (C.ParamIdMap.TryGetValue(expr.Id, out var parameterInfo))
                    {
                        return new EocVariableExpression(C, parameterInfo);
                    }
                    var localVarInfo = C.LocalIdMap[expr.Id];
                    return new EocVariableExpression(C, localVarInfo);

                case EplSystemId.Type_ClassMember:
                    var classVar = C.P.ClassVarIdMap[expr.Id];
                    return new EocVariableExpression(C, classVar);

                case EplSystemId.Type_Global:
                    var globalVar = C.P.GlobalVarIdMap[expr.Id];
                    return new EocVariableExpression(C, globalVar);

                case int x:
                    throw new Exception("未知变量类型：0x" + x.ToString("X8"));
            }
        }

        public EocVariableExpression(CodeConverter c, AbstractVariableInfo variableInfo) : base(c)
        {
            VariableInfo = variableInfo;
        }

        public AbstractVariableInfo VariableInfo { get; }

        public override CppTypeName GetResultType()
        {
            switch (VariableInfo)
            {
                case MethodParameterInfo v:
                    return P.GetCppTypeName(v.DataType, v.ArrayParameter);

                case LocalVariableInfo v:
                    return P.GetCppTypeName(v.DataType, v.UBound);

                case ClassVariableInfo v:
                    return P.GetCppTypeName(v.DataType, v.UBound);

                case GlobalVariableInfo v:
                    return P.GetCppTypeName(v.DataType, v.UBound);
            }
            throw new Exception("未知变量访问：" + P.IdToNameMap.GetUserDefinedName(VariableInfo.Id));
        }

        public override void WriteTo()
        {
            var name = P.GetUserDefinedName_SimpleCppName(VariableInfo.Id);
            switch (VariableInfo)
            {
                case MethodParameterInfo v:
                    if (v.OptionalParameter)
                    {
                        Writer.Write("eoc_value_");
                    }
                    Writer.Write(name);
                    break;

                case LocalVariableInfo v:
                    Writer.Write(name);
                    break;

                case ClassVariableInfo v:
                    if (EplSystemId.GetType(C.ClassItem.Id) == EplSystemId.Type_Class)
                    {
                        Writer.Write("this->");
                    }
                    else
                    {
                        Writer.Write(P.CmdNamespace);
                        Writer.Write("::");
                        Writer.Write(P.GetUserDefinedName_SimpleCppName(C.ClassItem.Id));
                        Writer.Write("::");
                    }
                    Writer.Write(name);
                    break;

                case GlobalVariableInfo v:
                    Writer.Write(P.GlobalNamespace);
                    Writer.Write("::");
                    Writer.Write(name);
                    break;

                default:
                    throw new Exception("未知变量访问：" + P.IdToNameMap.GetUserDefinedName(VariableInfo.Id));
            }
        }
    }
}