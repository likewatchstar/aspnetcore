// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Globalization;
using Microsoft.AspNetCore.Analyzers.DelegateEndpoints.Fixers;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace Microsoft.AspNetCore.Analyzers.DelegateEndpoints;

public static class CSharpDelegateEndpointsCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DelegateEndpointAnalyzer, new()
    where TCodeFix : DetectMismatchedParameterOptionalityFixer, new()
{
    public static DiagnosticResult Diagnostic(string diagnosticId = null)
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, XUnitVerifier>.Diagnostic(diagnosticId);

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => new DiagnosticResult(descriptor);

    public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpDelegateEndpointsAnalyzerVerifier<TAnalyzer>.Test { TestCode = source };
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    public static Task VerifyCodeFixAsync(string source, string fixedSource)
        => VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

    public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
        => VerifyCodeFixAsync(source, new[] { expected }, fixedSource);

    public static Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
        => VerifyCodeFixAsync(source, expected, fixedSource, string.Empty);

    public static Task VerifyCodeFixAsync(string sources, DiagnosticResult[] expected, string fixedSources, string usageSource = "")
    {
        var test = new DelegateEndpointAnalyzerTest
        {
            TestState =
            {
                Sources = { sources, usageSource },
                // We need to set the output type to an exe to properly
                // support top-level programs in the tests. Otherwise,
                // the test infra will assume we are trying to build a library.
                OutputKind = OutputKind.ConsoleApplication
            },
            FixedState =
            {
                Sources =  { fixedSources, usageSource }
            }
        };

        test.TestState.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    public class DelegateEndpointAnalyzerTest : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
    {
        public DelegateEndpointAnalyzerTest()
        {
            // We populate the ReferenceAssemblies used in the tests with the locally-built AspNetCore
            // assemblies that are referenced in a minimal app to ensure that there are no reference
            // errors during the build.
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60.AddAssemblies(ImmutableArray.Create(
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.WebApplication).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.DelegateEndpointRouteBuilderExtensions).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.IApplicationBuilder).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.IEndpointConventionBuilder).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.Extensions.Hosting.IHost).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Mvc.ModelBinding.IBinderTypeProviderMetadata).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Mvc.BindAttribute).Assembly.Location)));

            string TrimAssemblyExtension(string fullPath) => fullPath.Replace(".dll", string.Empty);
        }
    }
}