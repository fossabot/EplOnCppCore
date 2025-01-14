﻿using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EplOnCpp.Core.Statements;
using QIQI.EProjectFile;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QIQI.EplOnCpp.Core
{
    public class CodeConverter
    {
        public ProjectConverter P { get; }
        public string Name { get; }
        public string RefId { get; }
        public EocCmdInfo EocCmdInfo { get; }
        public bool IsClassMember { get; }
        public EocClass ClassItem { get; }
        public MethodInfo MethodItem { get; }
        public ILoggerWithContext Logger => P.Logger;
        public MethodParameterInfo[] Parameters { get; }
        public Dictionary<int, MethodParameterInfo> ParamIdMap { get; }
        public Dictionary<int, LocalVariableInfo> LocalIdMap { get; }
        public EocStatementBlock StatementBlock { get; set; }

        public CodeConverter(ProjectConverter projectConverter, EocClass classItem, MethodInfo methodItem)
        {
            this.P = projectConverter;
            this.Name = P.GetUserDefinedName_SimpleCppName(methodItem.Id);
            this.EocCmdInfo = P.GetEocCmdInfo(methodItem);
            this.IsClassMember = classItem is EocObjectClass;
            this.ClassItem = classItem;
            this.MethodItem = methodItem;
            this.Parameters = methodItem.Parameters;
            this.ParamIdMap = methodItem.Parameters.ToDictionary(x => x.Id);
            this.LocalIdMap = methodItem.Variables.ToDictionary(x => x.Id);
            if(IsClassMember)
                this.RefId = $"{ClassItem.CppName}|{EocCmdInfo.CppName}";
            else
                this.RefId = EocCmdInfo.CppName;
        }

        public void RemoveUnusedCode(HashSet<string> dependencies)
        {
            MethodItem.Variables = MethodItem.Variables.Where(x => dependencies.Contains($"{RefId}|{P.GetUserDefinedName_SimpleCppName(x.Id)}")).ToArray();
        }

        public void ParseCode()
        {
            using (new LoggerContextHelper(Logger)
                .Set("class", P.IdToNameMap.GetUserDefinedName(ClassItem.RawInfo.Id))
                .Set("method", P.IdToNameMap.GetUserDefinedName(MethodItem.Id)))
            {
                this.StatementBlock = EocStatementBlock.Translate(this, CodeDataParser.ParseStatementBlock(MethodItem.CodeData.ExpressionData, MethodItem.CodeData.Encoding));
            }
        }

        private int TempVarId = 0;

        public string AllocTempVar()
        {
            return $"eoc_temp{TempVarId++}";
        }

        public CodeConverter Optimize()
        {
            StatementBlock = StatementBlock.Optimize() as EocStatementBlock;
            return this;
        }

        private void WriteOptionalParameterReader(CodeWriter writer)
        {
            foreach (var x in Parameters.Where(x => x.OptionalParameter))
            {
                var name = P.GetUserDefinedName_SimpleCppName(x.Id);
                var realValueType = P.GetCppTypeName(x.DataType, x.ArrayParameter);
                var nullParameter = P.GetNullParameter(x.DataType, x.ArrayParameter);
                var initValue = P.GetInitValue(x.DataType, x.ArrayParameter);
                writer.NewLine();
                writer.Write($"bool eoc_isNull_{name} = !{name}.has_value();");
                if (x.ByRef || x.ArrayParameter || !P.IsValueType(x.DataType))
                {
                    writer.NewLine();
                    if (string.IsNullOrWhiteSpace(nullParameter))
                    {
                        writer.Write($"{realValueType} eoc_default_{name};");
                    }
                    else
                    {
                        writer.Write($"{realValueType} eoc_default_{name}({nullParameter});");
                    }

                    writer.NewLine();
                    writer.Write($"{realValueType}& eoc_value_{name} = eoc_isNull_{name} ? (eoc_default_{name} = {initValue}) : {name}.value().get();");
                }
                else
                {
                    writer.NewLine();
                    writer.Write($"{realValueType} eoc_value_{name} = eoc_isNull_{name} ? {initValue} : {name}.value();");
                }
            }
        }

        public void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph)
        {
            P.AnalyzeDependencies(graph, EocCmdInfo, RefId);
            foreach (var x in MethodItem.Variables)
            {
                var varRefId = $"{RefId}|{P.GetUserDefinedName_SimpleCppName(x.Id)}";
                P.AnalyzeDependencies(graph, varRefId, P.GetCppTypeName(x));
            }
            StatementBlock.AnalyzeDependencies(graph);
        }

        internal void DefineItem(CodeWriter writer)
        {
            var isVirtual = false;
            if (IsClassMember)
            {
                writer.NewLine();
                writer.Write(MethodItem.Public ? "public:" : "private:");
                if (MethodItem.Name != "_初始化" && MethodItem.Name != "_销毁")
                {
                    isVirtual = true;
                }
            }
            P.DefineMethod(writer, EocCmdInfo, Name, isVirtual);
        }

        internal void ImplementItem(CodeWriter writer)
        {
            using (new LoggerContextHelper(Logger)
                .Set("class", P.IdToNameMap.GetUserDefinedName(ClassItem.RawInfo.Id))
                .Set("method", P.IdToNameMap.GetUserDefinedName(MethodItem.Id)))
            {
                string classRawName = null;
                if (ClassItem is EocObjectClass x)
                    classRawName = x.RawName;
                P.WriteMethodHeader(writer, EocCmdInfo, Name, false, classRawName, false);
                using (writer.NewBlock())
                {
                    P.DefineVariable(writer, null, MethodItem.Variables);
                    WriteOptionalParameterReader(writer);
                    StatementBlock.WriteTo(writer);
                }
            }
        }

        public void WriteLetExpression(CodeWriter writer, EocExpression target, Action writeValue)
        {
            if (target is EocAccessMemberExpression expr && expr.MemberInfo.Setter != null)
            {
                expr.Target.WriteTo(writer);
                writer.Write("->");
                writer.Write(expr.MemberInfo.Setter);
                writer.Write("(");
                writeValue();
                writer.Write(")");
                return;
            }
            target.WriteTo(writer);
            writer.Write(" = ");
            writeValue();
        }
    }
}