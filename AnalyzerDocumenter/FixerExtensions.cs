using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace AnalyzerDocumenter
{
    public static class FixerExtensions
    {
        /// <summary>
        /// Get all the <see cref="CodeFixProvider"/>s that are implemented in the given <see cref="AnalyzerFileReference"/>
        /// </summary>
        /// <returns>An array of <see cref="CodeFixProvider"/>s</returns>
        public static ImmutableArray<CodeFixProvider> GetFixers(this AnalyzerFileReference analyzerFileReference)
        {
            if (analyzerFileReference == null)
            {
                return ImmutableArray<CodeFixProvider>.Empty;
            }

            ImmutableArray<CodeFixProvider>.Builder? builder = null;

            try
            {
                var analyzerAssembly = analyzerFileReference.GetAssembly();

                foreach (var typeInfo in analyzerAssembly.DefinedTypes)
                {
                    if (typeInfo.IsSubclassOf(typeof(CodeFixProvider)))
                    {
                        try
                        {
                            var attribute = typeInfo.GetCustomAttribute<ExportCodeFixProviderAttribute>();
                            if (attribute != null)
                            {
                                builder ??= ImmutableArray.CreateBuilder<CodeFixProvider>();
                                var fixer = (CodeFixProvider)Activator.CreateInstance(typeInfo.AsType())!;
                                if (HasImplementation(fixer))
                                {
                                    builder.Add(fixer);
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }

            return builder != null ? builder.ToImmutable() : ImmutableArray<CodeFixProvider>.Empty;
        }

        /// <summary>
        /// Check the method body of the Initialize method of an analyzer and if that's empty,
        /// then the analyzer hasn't been implemented yet.
        /// </summary>
        private static bool HasImplementation(CodeFixProvider fixer)
        {
            var method = fixer.GetType().GetTypeInfo().GetMethod("RegisterCodeFixesAsync");
            var stateMachineAttr = method?.GetCustomAttribute<AsyncStateMachineAttribute>();
            var moveNextMethod = stateMachineAttr?.StateMachineType.GetTypeInfo().GetDeclaredMethod("MoveNext");
            if (moveNextMethod != null)
            {
                var body = moveNextMethod.GetMethodBody();
                var ilInstructionCount = body?.GetILAsByteArray()?.Length;
                return ilInstructionCount != 177;
            }

            return true;
        }
    }
}
