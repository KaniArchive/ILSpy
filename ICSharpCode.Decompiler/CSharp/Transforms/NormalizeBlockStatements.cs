using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Syntax.PatternMatching;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	class NormalizeBlockStatements : DepthFirstAstVisitor, IAstTransform
	{
		TransformContext context;
		bool hasNamespace;
		NamespaceDeclaration singleNamespaceDeclaration;

		public override void VisitSyntaxTree(SyntaxTree syntaxTree)
		{
			singleNamespaceDeclaration = null;
			hasNamespace = false;
			base.VisitSyntaxTree(syntaxTree);
			if (context.Settings.FileScopedNamespaces && singleNamespaceDeclaration != null)
			{
				singleNamespaceDeclaration.IsFileScoped = true;
			}
		}

		public override void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
		{
			singleNamespaceDeclaration = null;
			if (!hasNamespace)
			{
				hasNamespace = true;
				singleNamespaceDeclaration = namespaceDeclaration;
			}
			base.VisitNamespaceDeclaration(namespaceDeclaration);

		}

		public override void VisitIfElseStatement(IfElseStatement ifElseStatement)
		{
			base.VisitIfElseStatement(ifElseStatement);
			DoTransform(ifElseStatement.TrueStatement, ifElseStatement);
			DoTransform(ifElseStatement.FalseStatement, ifElseStatement);
		}

		public override void VisitWhileStatement(WhileStatement whileStatement)
		{
			base.VisitWhileStatement(whileStatement);
			InsertBlock(whileStatement.EmbeddedStatement);
		}

		public override void VisitDoWhileStatement(DoWhileStatement doWhileStatement)
		{
			base.VisitDoWhileStatement(doWhileStatement);
			InsertBlock(doWhileStatement.EmbeddedStatement);
		}

		public override void VisitForeachStatement(ForeachStatement foreachStatement)
		{
			base.VisitForeachStatement(foreachStatement);
			InsertBlock(foreachStatement.EmbeddedStatement);
		}

		public override void VisitForStatement(ForStatement forStatement)
		{
			base.VisitForStatement(forStatement);
			InsertBlock(forStatement.EmbeddedStatement);
		}

		public override void VisitFixedStatement(FixedStatement fixedStatement)
		{
			base.VisitFixedStatement(fixedStatement);
			InsertBlock(fixedStatement.EmbeddedStatement);
		}

		public override void VisitLockStatement(LockStatement lockStatement)
		{
			base.VisitLockStatement(lockStatement);
			InsertBlock(lockStatement.EmbeddedStatement);
		}

		public override void VisitBlockStatement(BlockStatement blockStatement)
		{
			base.VisitBlockStatement(blockStatement);
			TransformTailGotoReturns(blockStatement);
			TransformUnassignedGotoStateDispatch(blockStatement);
		}

		public override void VisitUsingStatement(UsingStatement usingStatement)
		{
			base.VisitUsingStatement(usingStatement);
			DoTransform(usingStatement.EmbeddedStatement, usingStatement);
		}

		void DoTransform(Statement statement, Statement parent)
		{
			if (statement.IsNull)
				return;
			if (context.Settings.AlwaysUseBraces)
			{
				if (!IsElseIf(statement, parent))
				{
					InsertBlock(statement);
				}
			}
			else
			{
				if (statement is BlockStatement b && b.Statements.Count == 1 && IsAllowedAsEmbeddedStatement(b.Statements.First(), parent))
				{
					statement.ReplaceWith(b.Statements.First().Detach());
				}
				else if (!IsAllowedAsEmbeddedStatement(statement, parent))
				{
					InsertBlock(statement);
				}
			}
		}

		bool IsElseIf(Statement statement, Statement parent)
		{
			return parent is IfElseStatement && statement.Role == IfElseStatement.FalseRole;
		}

		static void InsertBlock(Statement statement)
		{
			if (statement.IsNull)
				return;
			if (!(statement is BlockStatement))
			{
				var b = new BlockStatement();
				statement.ReplaceWith(b);
				if (statement is EmptyStatement && !statement.Children.Any())
				{
					b.CopyAnnotationsFrom(statement);
				}
				else
				{
					b.Add(statement);
				}
			}
		}

		bool IsAllowedAsEmbeddedStatement(Statement statement, Statement parent)
		{
			switch (statement)
			{
				case IfElseStatement ies:
					return parent is IfElseStatement && ies.Role == IfElseStatement.FalseRole;
				case VariableDeclarationStatement vds:
				case WhileStatement ws:
				case DoWhileStatement dws:
				case SwitchStatement ss:
				case ForeachStatement fes:
				case ForStatement fs:
				case LockStatement ls:
				case FixedStatement fxs:
					return false;
				case UsingStatement us:
					return parent is UsingStatement && !us.IsEnhanced;
				default:
					return !(parent?.Parent is IfElseStatement);
			}
		}

		static void TransformUnassignedGotoStateDispatch(BlockStatement blockStatement)
		{
			var statements = blockStatement.Statements.ToList();
			for (int i = 0; i + 2 < statements.Count; i++)
			{
				if (statements[i] is not LabelStatement label)
					continue;
				if (statements[i + 1] is not VariableDeclarationStatement { Variables.Count: 1 } declaration)
					continue;
				string stateVariable = declaration.Variables.Single().Name;
				if (statements[i + 2] is not IfElseStatement ifStatement)
					continue;
				if (!IsStateReturnCheck(ifStatement, stateVariable, out var returnExpression))
					continue;

				var gotos = blockStatement.Descendants
					.OfType<GotoStatement>()
					.Where(gotoStatement => gotoStatement.Label == label.Label)
					.ToList();
				if (gotos.Count == 0)
					continue;
				foreach (var gotoStatement in gotos)
				{
					ReplaceStateGoto(gotoStatement, returnExpression);
				}
				statements[i].Remove();
				statements[i + 1].Remove();
				statements[i + 2].Remove();
				if (i < statements.Count - 3 && IsNullAssignmentTo(statements[i + 3], returnExpression))
					statements[i + 3].Remove();
				return;
			}
		}

		static void TransformTailGotoReturns(BlockStatement blockStatement)
		{
			var statements = blockStatement.Statements.ToList();
			if (statements.Count < 3)
				return;
			if (statements[^2] is not LabelStatement label)
				return;
			if (statements[^1] is not ReturnStatement {
				Expression: IdentifierExpression returnIdentifier
			} returnStatement)
				return;
			if (!IsUnconditionalExit(statements[^3]))
				return;

			var returnVariable = returnIdentifier.GetILVariable();
			if (returnVariable == null)
				return;
			var gotos = blockStatement.Descendants
				.OfType<GotoStatement>()
				.Where(gotoStatement => gotoStatement.Label == label.Label)
				.ToList();
			if (gotos.Count == 0)
				return;
			var assignments = new List<(GotoStatement Goto, ExpressionStatement Statement, Expression Value)>();
			foreach (var gotoStatement in gotos)
			{
				if (gotoStatement.PrevSibling is not ExpressionStatement {
					Expression: AssignmentExpression {
						Operator: AssignmentOperatorType.Assign,
						Left: IdentifierExpression assignedIdentifier,
						Right: Expression assignedValue
					}
				} assignmentStatement)
					return;
				if (assignedIdentifier.GetILVariable() != returnVariable)
					return;
				assignments.Add((gotoStatement, assignmentStatement, assignedValue));
			}
			if (blockStatement.DescendantsAndSelf
				.OfType<IdentifierExpression>()
				.Count(identifier => identifier.GetILVariable() == returnVariable) != assignments.Count + 1)
				return;

			foreach (var assignment in assignments)
			{
				assignment.Statement.ReplaceWith(new ReturnStatement(assignment.Value.Detach()).CopyAnnotationsFrom(assignment.Goto));
				assignment.Goto.Remove();
			}
			label.Remove();
			returnStatement.Remove();
			RemoveUninitializedDeclaration(blockStatement, returnVariable);
		}

		static bool IsUnconditionalExit(Statement statement)
		{
			return statement switch {
				ReturnStatement => true,
				ThrowStatement => true,
				GotoStatement => true,
				BreakStatement => true,
				ContinueStatement => true,
				BlockStatement block => block.Statements.LastOrDefault() is { } lastStatement && IsUnconditionalExit(lastStatement),
				_ => false
			};
		}

		static void RemoveUninitializedDeclaration(BlockStatement blockStatement, IL.ILVariable variable)
		{
			if (blockStatement.DescendantsAndSelf
				.OfType<IdentifierExpression>()
				.Any(identifier => identifier.GetILVariable() == variable))
				return;
			foreach (var declaration in blockStatement.Statements.OfType<VariableDeclarationStatement>())
			{
				if (declaration.Variables.Count != 1)
					continue;
				var initializer = declaration.Variables.Single();
				if (!initializer.Initializer.IsNull)
					continue;
				if (initializer.GetILVariable() != variable)
					continue;
				declaration.Remove();
				return;
			}
		}

		static bool IsStateReturnCheck(IfElseStatement ifStatement, string stateVariable, out Expression returnExpression)
		{
			returnExpression = null;
			if (ifStatement.Condition is not BinaryOperatorExpression { Operator: BinaryOperatorType.Equality } condition)
				return false;
			if (condition.Left is not IdentifierExpression identifier || identifier.Identifier != stateVariable)
				return false;
			if (condition.Right is not PrimitiveExpression { Value: int })
				return false;
			Statement trueStatement = ifStatement.TrueStatement;
			if (trueStatement is BlockStatement { Statements.Count: 1 } block)
				trueStatement = block.Statements.Single();
			if (trueStatement is not ReturnStatement returnStatement || returnStatement.Expression.IsNull)
				return false;
			returnExpression = returnStatement.Expression;
			return true;
		}

		static bool IsNullAssignmentTo(Statement statement, Expression returnExpression)
		{
			if (returnExpression is not IdentifierExpression returnIdentifier)
				return false;
			return statement is ExpressionStatement {
				Expression: AssignmentExpression {
					Left: IdentifierExpression assignedIdentifier,
					Right: NullReferenceExpression
				}
			} && assignedIdentifier.Identifier == returnIdentifier.Identifier;
		}

		static void ReplaceStateGoto(GotoStatement gotoStatement, Expression returnExpression)
		{
			if (returnExpression is IdentifierExpression returnIdentifier
				&& gotoStatement.PrevSibling is ExpressionStatement {
					Expression: AssignmentExpression {
						Left: IdentifierExpression assignedIdentifier,
						Right: var assignedValue
					}
				} assignmentStatement
				&& assignedIdentifier.Identifier == returnIdentifier.Identifier)
			{
				assignmentStatement.ReplaceWith(new ReturnStatement(assignedValue.Detach()).CopyAnnotationsFrom(gotoStatement));
				gotoStatement.Remove();
				return;
			}
			gotoStatement.ReplaceWith(new ContinueStatement().CopyAnnotationsFrom(gotoStatement));
		}

		void IAstTransform.Run(AstNode rootNode, TransformContext context)
		{
			this.context = context;
			rootNode.AcceptVisitor(this);
		}

		public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
		{
			if (context.Settings.UseExpressionBodyForCalculatedGetterOnlyProperties)
			{
				SimplifyPropertyDeclaration(propertyDeclaration);
			}
			base.VisitPropertyDeclaration(propertyDeclaration);
		}

		public override void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
		{
			if (context.Settings.UseExpressionBodyForCalculatedGetterOnlyProperties)
			{
				SimplifyIndexerDeclaration(indexerDeclaration);
			}
			base.VisitIndexerDeclaration(indexerDeclaration);
		}

		static readonly PropertyDeclaration CalculatedGetterOnlyPropertyPattern = new PropertyDeclaration() {
			Attributes = { new Repeat(new AnyNode()) },
			Modifiers = Modifiers.Any,
			Name = Pattern.AnyString,
			PrivateImplementationType = new AnyNodeOrNull(),
			ReturnType = new AnyNode(),
			Getter = new Accessor() {
				Modifiers = Modifiers.Any,
				Body = new BlockStatement() { new ReturnStatement(new AnyNode("expression")) }
			}
		};

		static readonly IndexerDeclaration CalculatedGetterOnlyIndexerPattern = new IndexerDeclaration() {
			Attributes = { new Repeat(new AnyNode()) },
			Modifiers = Modifiers.Any,
			PrivateImplementationType = new AnyNodeOrNull(),
			Parameters = { new Repeat(new AnyNode()) },
			ReturnType = new AnyNode(),
			Getter = new Accessor() {
				Modifiers = Modifiers.Any,
				Body = new BlockStatement() { new ReturnStatement(new AnyNode("expression")) }
			}
		};

		/// <summary>
		/// Modifiers that are emitted on accessors, but can be moved to the property declaration.
		/// </summary>
		const Modifiers movableModifiers = Modifiers.Readonly;

		void SimplifyPropertyDeclaration(PropertyDeclaration propertyDeclaration)
		{
			var m = CalculatedGetterOnlyPropertyPattern.Match(propertyDeclaration);
			if (!m.Success)
				return;
			if ((propertyDeclaration.Getter.Modifiers & ~movableModifiers) != 0)
				return;
			propertyDeclaration.Modifiers |= propertyDeclaration.Getter.Modifiers;
			propertyDeclaration.ExpressionBody = m.Get<Expression>("expression").Single().Detach();
			propertyDeclaration.CopyAnnotationsFrom(propertyDeclaration.Getter);
			propertyDeclaration.Getter.Remove();
		}

		void SimplifyIndexerDeclaration(IndexerDeclaration indexerDeclaration)
		{
			var m = CalculatedGetterOnlyIndexerPattern.Match(indexerDeclaration);
			if (!m.Success)
				return;
			if ((indexerDeclaration.Getter.Modifiers & ~movableModifiers) != 0)
				return;
			indexerDeclaration.Modifiers |= indexerDeclaration.Getter.Modifiers;
			indexerDeclaration.ExpressionBody = m.Get<Expression>("expression").Single().Detach();
			indexerDeclaration.CopyAnnotationsFrom(indexerDeclaration.Getter);
			indexerDeclaration.Getter.Remove();
		}
	}
}
