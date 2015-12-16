using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Vbe.Interop;
using Rubberduck.Parsing.Grammar;
using Rubberduck.Parsing.Nodes;
using Rubberduck.Parsing.Symbols;
using Rubberduck.VBEditor;
using Rubberduck.VBEditor.Extensions;
using Rubberduck.VBA;

namespace Rubberduck.Parsing.VBA
{
    public class RubberduckParser : IRubberduckParser
    {
        public RubberduckParser(RubberduckParserState state)
        {
            _state = state;
        }

        private readonly RubberduckParserState _state;
        public RubberduckParserState State { get { return _state; } }

        public async Task ParseAsync(VBComponent vbComponent, CancellationToken token)
        {
            var component = vbComponent;
            var name = component.Name;

            Debug.Print("Starting parser task for component '{0}'.", name);
            var parseTask = Task.Run(() => ParseInternal(component, token), token);

            try
            {
                await parseTask;
                Debug.Print("Parse task completed for component '{0}'.", name);
            }
            catch (SyntaxErrorException exception)
            {
                Debug.Print("A SyntaxErrorException was thrown while parsing component '{0}'.", name);
                Debug.Print(exception.ToString());
                State.SetModuleState(component, ParserState.Error, exception);
            }
            catch (OperationCanceledException)
            {
                Debug.Print("OperationCanceledException was thrown while parsing component '{0}'", name);
                // no need to blow up, we're still in command.
            } 
        }

        public void Resolve(CancellationToken token)
        {
            var options = new ParallelOptions { CancellationToken = token };
            Parallel.ForEach(_state.ParseTrees, options, kvp =>
            {
                ResolveReferences(kvp.Key, kvp.Value, token);
            });
        }

        private IEnumerable<CommentNode> ParseComments(QualifiedModuleName qualifiedName)
        {
            var code = qualifiedName.Component.CodeModule.Code();
            var commentBuilder = new StringBuilder();
            var continuing = false;

            var startLine = 0;
            var startColumn = 0;

            for (var i = 0; i < code.Length; i++)
            {
                var line = code[i];
                var index = 0;

                if (continuing || line.HasComment(out index))
                {
                    startLine = continuing ? startLine : i;
                    startColumn = continuing ? startColumn : index;

                    var commentLength = line.Length - index;

                    continuing = line.EndsWith("_");
                    if (!continuing)
                    {
                        commentBuilder.Append(line.Substring(index, commentLength).TrimStart());
                        var selection = new Selection(startLine + 1, startColumn + 1, i + 1, line.Length + 1);

                        var result = new CommentNode(commentBuilder.ToString(), new QualifiedSelection(qualifiedName, selection));
                        commentBuilder.Clear();

                        yield return result;
                    }
                    else
                    {
                        // ignore line continuations in comment text:
                        commentBuilder.Append(line.Substring(index, commentLength).TrimStart());
                    }
                }
            }
        }

        private void ParseInternal(VBComponent vbComponent, CancellationToken token)
        {
            var name = vbComponent.Name;
            _state.ClearDeclarations(vbComponent);
            State.SetModuleState(vbComponent, ParserState.Parsing);
            Debug.Print("Component '{0}' is in '{1}' state (token:{2}).", name, ParserState.Parsing, token.GetHashCode());

            var qualifiedName = new QualifiedModuleName(vbComponent);
            Debug.Print("Parsing comments in component '{0}'.", name);
            _state.SetModuleComments(vbComponent, ParseComments(qualifiedName));

            var obsoleteCallsListener = new ObsoleteCallStatementListener();
            var obsoleteLetListener = new ObsoleteLetStatementListener();

            var listeners = new IParseTreeListener[]
            {
                obsoleteCallsListener,
                obsoleteLetListener
            };

            if (token.IsCancellationRequested)
            {
                Debug.Print("Cancellation requested, aborting parse task of component '{0}' (token:{1}).", name, token.GetHashCode());
                _state.SetModuleState(vbComponent, ParserState.Error);
                token.ThrowIfCancellationRequested();
            }

            ITokenStream stream;
            var code = string.Join("\r\n", vbComponent.CodeModule.Code());
            var tree = ParseInternal(code, listeners, out stream);
            Debug.Print("IParseTree acquired for component '{0}'.", name);

            if (token.IsCancellationRequested)
            {
                Debug.Print("Cancellation requested, aborting parse task of component '{0}' (token:{1}).", name, token.GetHashCode());
                _state.SetModuleState(vbComponent, ParserState.Error);
                token.ThrowIfCancellationRequested();
            }

            _state.AddTokenStream(vbComponent, stream);
            _state.AddParseTree(vbComponent, tree);

            // cannot locate declarations in one pass *the way it's currently implemented*,
            // because the context in EnterSubStmt() doesn't *yet* have child nodes when the context enters.
            // so we need to EnterAmbiguousIdentifier() and evaluate the parent instead - this *might* work.
            var declarationsListener = new DeclarationSymbolsListener(qualifiedName, Accessibility.Implicit, vbComponent.Type, _state.Comments, token);

            declarationsListener.NewDeclaration += declarationsListener_NewDeclaration;
            declarationsListener.CreateModuleDeclarations();

            if (token.IsCancellationRequested)
            {
                Debug.Print("Cancellation requested, aborting first pass walking IParseTree of component '{0}' (token:{1}).", name, token.GetHashCode());
                _state.SetModuleState(vbComponent, ParserState.Error);
                token.ThrowIfCancellationRequested();
            }

            var walker = new ParseTreeWalker();
            walker.Walk(declarationsListener, tree);
            declarationsListener.NewDeclaration -= declarationsListener_NewDeclaration;

            _state.ObsoleteCallContexts = obsoleteCallsListener.Contexts.Select(context => new QualifiedContext(qualifiedName, context));
            _state.ObsoleteLetContexts = obsoleteLetListener.Contexts.Select(context => new QualifiedContext(qualifiedName, context));

            State.SetModuleState(vbComponent, ParserState.Parsed);
        }

        private IParseTree ParseInternal(string code, IEnumerable<IParseTreeListener> listeners, out ITokenStream outStream)
        {
            var input = new AntlrInputStream(code);
            var lexer = new VBALexer(input);
            var tokens = new CommonTokenStream(lexer);
            var parser = new VBAParser(tokens);

            parser.AddErrorListener(new ExceptionErrorListener());
            foreach (var listener in listeners)
            {
                parser.AddParseListener(listener);
            }

            outStream = tokens;
            return parser.startRule();
        }

        private void declarationsListener_NewDeclaration(object sender, DeclarationEventArgs e)
        {
             _state.AddDeclaration(e.Declaration);
        }

        private void ResolveReferences(VBComponent component, IParseTree tree, CancellationToken token)
        {
            var state = _state.GetModuleState(component);
            if (state != ParserState.Parsed)
            {
                Debug.Print("Component '{0}' has state '{1}', aborting resolver task. (Token:{2})", component.Name, state, token.GetHashCode());
                return; //throw new InvalidOperationException("Resolver task was invoked for a module that didn't successfully parse.");
            }

            _state.SetModuleState(component, ParserState.Resolving);
            var declarations = _state.AllDeclarations;

            var resolver = new IdentifierReferenceResolver(new QualifiedModuleName(component), declarations);
            var listener = new IdentifierReferenceListener(resolver, token);
            var walker = new ParseTreeWalker();
            try
            {
                walker.Walk(listener, tree);
            }
            catch(WalkerCancelledException exception)
            {
                Debug.Print("An exception was thrown in the resolver. Reporting error state for component '{0}'.", component.Name);
                _state.SetModuleState(component, ParserState.Error, exception);
                return;
            }

            Debug.Print("Identifier references successfully resolved in component '{0}'.", component.Name);
            _state.SetModuleState(component, ParserState.Ready);
            Debug.Print("Component '{0}' has state '{1}'.", component.Name, _state.GetModuleState(component));
        }

        private class ObsoleteCallStatementListener : VBABaseListener
        {
            private readonly IList<VBAParser.ExplicitCallStmtContext> _contexts = new List<VBAParser.ExplicitCallStmtContext>();
            public IEnumerable<VBAParser.ExplicitCallStmtContext> Contexts { get { return _contexts; } }

            public override void EnterExplicitCallStmt(VBAParser.ExplicitCallStmtContext context)
            {
                var procedureCall = context.eCS_ProcedureCall();
                if (procedureCall != null)
                {
                    if (procedureCall.CALL() != null)
                    {
                        _contexts.Add(context);
                        return;
                    }
                }

                var memberCall = context.eCS_MemberProcedureCall();
                if (memberCall == null) return;
                if (memberCall.CALL() == null) return;
                _contexts.Add(context);
            }
        }

        private class ObsoleteLetStatementListener : VBABaseListener
        {
            private readonly IList<VBAParser.LetStmtContext> _contexts = new List<VBAParser.LetStmtContext>();
            public IEnumerable<VBAParser.LetStmtContext> Contexts { get { return _contexts; } }

            public override void EnterLetStmt(VBAParser.LetStmtContext context)
            {
                if (context.LET() != null)
                {
                    _contexts.Add(context);
                }
            }
        }
    }
}
