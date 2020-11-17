﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.InvertConditional
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InvertConditional
    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertConditional), [Shared]>
    Friend Class VisualBasicInvertConditionalCodeRefactoringProvider
        Inherits AbstractInvertConditionalCodeRefactoringProvider(Of TernaryConditionalExpressionSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function ShouldOffer(
            conditional As TernaryConditionalExpressionSyntax) As Boolean

            Return Not conditional.FirstCommaToken.IsMissing AndAlso
                   Not conditional.SecondCommaToken.IsMissing AndAlso
                   Not conditional.CloseParenToken.IsMissing
        End Function
    End Class
End Namespace
