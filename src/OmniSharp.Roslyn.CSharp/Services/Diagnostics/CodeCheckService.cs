﻿using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using OmniSharp.Mef;
using OmniSharp.Models;

namespace OmniSharp.Roslyn.CSharp.Services.Diagnostics
{
    [OmniSharpHandler(OmnisharpEndpoints.CodeCheck, LanguageNames.CSharp)]
    public class CodeCheckService : RequestHandler<CodeCheckRequest, QuickFixResponse>
    {
        private OmniSharpWorkspace _workspace;

        [ImportingConstructor]
        public CodeCheckService(OmniSharpWorkspace workspace)
        {
            _workspace = workspace;
        }

        public async Task<QuickFixResponse> Handle(CodeCheckRequest request)
        {
            var quickFixes = new List<QuickFix>();

            var documents = request.FileName != null
                ? _workspace.GetDocuments(request.FileName)
                : _workspace.CurrentSolution.Projects.SelectMany(project => project.Documents);

            foreach (var document in documents)
            {
                var semanticModel = await document.GetSemanticModelAsync();
                IEnumerable<Diagnostic> diagnostics = semanticModel.GetDiagnostics();

                if (document.SourceCodeKind != SourceCodeKind.Regular)
                {
                    // CS8099 needs to be surpressed so that we can use #load directives in scripts
                    // additionally, we need to suppress CS1701: https://github.com/dotnet/roslyn/issues/5501
                    diagnostics = diagnostics.Where(diagnostic => diagnostic.Id != "CS8099" && diagnostic.Id != "CS1701");
                }

                foreach (var quickFix in diagnostics.Select(MakeQuickFix))
                {
                    var existingQuickFix = quickFixes.FirstOrDefault(q => q.Equals(quickFix));
                    if (existingQuickFix == null)
                    {
                        quickFix.Projects.Add(document.Project.Name);
                        quickFixes.Add(quickFix);
                    }
                    else
                    {
                        existingQuickFix.Projects.Add(document.Project.Name);
                    }
                }
            }

            return new QuickFixResponse(quickFixes);
        }

        private static QuickFix MakeQuickFix(Diagnostic diagnostic)
        {
            var span = diagnostic.Location.GetMappedLineSpan();
            return new DiagnosticLocation
            {
                FileName = span.Path,
                Line = span.StartLinePosition.Line,
                Column = span.StartLinePosition.Character,
                EndLine = span.EndLinePosition.Line,
                EndColumn = span.EndLinePosition.Character,
                Text = diagnostic.GetMessage(),
                LogLevel = diagnostic.Severity.ToString()
            };
        }
    }
}
