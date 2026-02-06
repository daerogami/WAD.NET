using System.Collections.Generic;
using System.Collections.Immutable;

namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// Abstract base class for all state-related syntax nodes within a States block.
/// </summary>
public abstract class StateSyntax : SyntaxNode
{
    protected StateSyntax(TextSpan span, TextSpan fullSpan)
        : base(span, fullSpan)
    {
    }
}

/// <summary>
/// A state label such as <c>Spawn:</c> or <c>See.Init:</c>.
/// </summary>
public sealed class StateLabelSyntax : StateSyntax
{
    /// <summary>
    /// The label identifier token.
    /// </summary>
    public SyntaxToken Identifier { get; }

    /// <summary>
    /// The optional <c>.</c> token for sub-labels.
    /// </summary>
    public SyntaxToken? DotToken { get; }

    /// <summary>
    /// The optional sub-label identifier token.
    /// </summary>
    public SyntaxToken? SubIdentifier { get; }

    /// <summary>
    /// The <c>:</c> token.
    /// </summary>
    public SyntaxToken ColonToken { get; }

    /// <summary>
    /// The full label name, including any sub-label (e.g., <c>See.Init</c>).
    /// </summary>
    public string LabelName =>
        SubIdentifier.HasValue ? $"{Identifier.Text}.{SubIdentifier.Value.Text}" : Identifier.Text;

    public override SyntaxNodeKind Kind => SyntaxNodeKind.StateLabel;

    public StateLabelSyntax(
        SyntaxToken identifier,
        SyntaxToken? dotToken,
        SyntaxToken? subIdentifier,
        SyntaxToken colonToken)
        : base(
            ComputeSpan(identifier.Span, colonToken.Span),
            ComputeSpan(identifier.FullSpan, colonToken.FullSpan))
    {
        Identifier = identifier;
        DotToken = dotToken;
        SubIdentifier = subIdentifier;
        ColonToken = colonToken;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield break;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return Identifier;
        if (DotToken.HasValue) yield return DotToken.Value;
        if (SubIdentifier.HasValue) yield return SubIdentifier.Value;
        yield return ColonToken;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitStateLabel(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitStateLabel(this);

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A state frame definition such as <c>TNT1 A 1 Bright A_Look</c>.
/// </summary>
public sealed class StateFrameSyntax : StateSyntax
{
    /// <summary>
    /// The 4-character sprite name identifier token.
    /// </summary>
    public SyntaxToken SpriteIdentifier { get; }

    /// <summary>
    /// The frame letter(s) token.
    /// </summary>
    public SyntaxToken FrameLetters { get; }

    /// <summary>
    /// The duration token (tic count or expression).
    /// </summary>
    public SyntaxToken Duration { get; }

    /// <summary>
    /// Modifier tokens such as <c>Bright</c>, <c>Offset</c>, etc.
    /// </summary>
    public ImmutableArray<SyntaxToken> Modifiers { get; }

    /// <summary>
    /// The optional action to execute on this frame.
    /// </summary>
    public StateActionSyntax? Action { get; }

    /// <summary>
    /// The sprite name text.
    /// </summary>
    public string SpriteName => SpriteIdentifier.Text;

    /// <summary>
    /// The frame letter(s) text.
    /// </summary>
    public string Frames => FrameLetters.Text;

    /// <summary>
    /// The duration in tics (from the token value, if available).
    /// </summary>
    public int? DurationTics => Duration.Value as int?;

    /// <summary>
    /// Returns <see langword="true"/> if the <c>Bright</c> modifier is present.
    /// </summary>
    public bool IsBright
    {
        get
        {
            foreach (var mod in Modifiers)
            {
                if (string.Equals(mod.Text, "Bright", System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.StateFrame;

    public StateFrameSyntax(
        SyntaxToken spriteIdentifier,
        SyntaxToken frameLetters,
        SyntaxToken duration,
        ImmutableArray<SyntaxToken> modifiers,
        StateActionSyntax? action)
        : base(
            ComputeSpanForFrame(spriteIdentifier, duration, modifiers, action),
            ComputeFullSpanForFrame(spriteIdentifier, duration, modifiers, action))
    {
        SpriteIdentifier = spriteIdentifier;
        FrameLetters = frameLetters;
        Duration = duration;
        Modifiers = modifiers;
        Action = action;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        if (Action != null) yield return Action;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return SpriteIdentifier;
        yield return FrameLetters;
        yield return Duration;
        foreach (var mod in Modifiers)
            yield return mod;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitStateFrame(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitStateFrame(this);

    private static TextSpan ComputeSpanForFrame(
        SyntaxToken spriteIdentifier,
        SyntaxToken duration,
        ImmutableArray<SyntaxToken> modifiers,
        StateActionSyntax? action)
    {
        TextSpan last;
        if (action != null) last = action.Span;
        else if (modifiers.Length > 0) last = modifiers[modifiers.Length - 1].Span;
        else last = duration.Span;
        return ComputeSpan(spriteIdentifier.Span, last);
    }

    private static TextSpan ComputeFullSpanForFrame(
        SyntaxToken spriteIdentifier,
        SyntaxToken duration,
        ImmutableArray<SyntaxToken> modifiers,
        StateActionSyntax? action)
    {
        TextSpan last;
        if (action != null) last = action.FullSpan;
        else if (modifiers.Length > 0) last = modifiers[modifiers.Length - 1].FullSpan;
        else last = duration.FullSpan;
        return ComputeSpan(spriteIdentifier.FullSpan, last);
    }

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A state action, which can be either a named action call or an anonymous block.
/// </summary>
public sealed class StateActionSyntax : SyntaxNode
{
    /// <summary>
    /// The optional action function identifier (null for anonymous blocks).
    /// </summary>
    public SyntaxToken? ActionIdentifier { get; }

    /// <summary>
    /// The optional <c>(</c> token.
    /// </summary>
    public SyntaxToken? OpenParenToken { get; }

    /// <summary>
    /// The arguments to the action function.
    /// </summary>
    public ImmutableArray<ArgumentSyntax> Arguments { get; }

    /// <summary>
    /// The optional <c>)</c> token.
    /// </summary>
    public SyntaxToken? CloseParenToken { get; }

    /// <summary>
    /// The optional anonymous action block.
    /// </summary>
    public BlockStatementSyntax? ActionBlock { get; }

    /// <summary>
    /// Returns <see langword="true"/> if this is an anonymous block action.
    /// </summary>
    public bool IsAnonymousBlock => ActionBlock != null && !ActionIdentifier.HasValue;

    public override SyntaxNodeKind Kind => SyntaxNodeKind.StateAction;

    public StateActionSyntax(
        SyntaxToken? actionIdentifier,
        SyntaxToken? openParenToken,
        ImmutableArray<ArgumentSyntax> arguments,
        SyntaxToken? closeParenToken,
        BlockStatementSyntax? actionBlock)
        : base(
            ComputeSpanForAction(actionIdentifier, openParenToken, closeParenToken, actionBlock),
            ComputeFullSpanForAction(actionIdentifier, openParenToken, closeParenToken, actionBlock))
    {
        ActionIdentifier = actionIdentifier;
        OpenParenToken = openParenToken;
        Arguments = arguments;
        CloseParenToken = closeParenToken;
        ActionBlock = actionBlock;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        foreach (var arg in Arguments)
            yield return arg;
        if (ActionBlock != null) yield return ActionBlock;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        if (ActionIdentifier.HasValue) yield return ActionIdentifier.Value;
        if (OpenParenToken.HasValue) yield return OpenParenToken.Value;
        if (CloseParenToken.HasValue) yield return CloseParenToken.Value;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitStateAction(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitStateAction(this);

    private static TextSpan ComputeSpanForAction(
        SyntaxToken? actionIdentifier,
        SyntaxToken? openParenToken,
        SyntaxToken? closeParenToken,
        BlockStatementSyntax? actionBlock)
    {
        TextSpan first;
        if (actionIdentifier.HasValue) first = actionIdentifier.Value.Span;
        else if (actionBlock != null) first = actionBlock.Span;
        else if (openParenToken.HasValue) first = openParenToken.Value.Span;
        else first = new TextSpan(0, 0);

        TextSpan last;
        if (actionBlock != null) last = actionBlock.Span;
        else if (closeParenToken.HasValue) last = closeParenToken.Value.Span;
        else if (actionIdentifier.HasValue) last = actionIdentifier.Value.Span;
        else last = first;

        return ComputeSpan(first, last);
    }

    private static TextSpan ComputeFullSpanForAction(
        SyntaxToken? actionIdentifier,
        SyntaxToken? openParenToken,
        SyntaxToken? closeParenToken,
        BlockStatementSyntax? actionBlock)
    {
        TextSpan first;
        if (actionIdentifier.HasValue) first = actionIdentifier.Value.FullSpan;
        else if (actionBlock != null) first = actionBlock.FullSpan;
        else if (openParenToken.HasValue) first = openParenToken.Value.FullSpan;
        else first = new TextSpan(0, 0);

        TextSpan last;
        if (actionBlock != null) last = actionBlock.FullSpan;
        else if (closeParenToken.HasValue) last = closeParenToken.Value.FullSpan;
        else if (actionIdentifier.HasValue) last = actionIdentifier.Value.FullSpan;
        else last = first;

        return ComputeSpan(first, last);
    }

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A <c>Goto</c> state directive such as <c>Goto See</c> or <c>Goto Super::Spawn+1</c>.
/// </summary>
public sealed class StateGotoSyntax : StateSyntax
{
    /// <summary>
    /// The <c>Goto</c> keyword.
    /// </summary>
    public SyntaxToken GotoKeyword { get; }

    /// <summary>
    /// The optional class scope identifier (e.g., <c>Super</c> in <c>Super::Spawn</c>).
    /// </summary>
    public SyntaxToken? ClassIdentifier { get; }

    /// <summary>
    /// The optional <c>::</c> scope resolution token.
    /// </summary>
    public SyntaxToken? ColonColonToken { get; }

    /// <summary>
    /// The target label identifier.
    /// </summary>
    public SyntaxToken LabelIdentifier { get; }

    /// <summary>
    /// The optional <c>+</c> token for frame offset.
    /// </summary>
    public SyntaxToken? PlusToken { get; }

    /// <summary>
    /// The optional frame offset token.
    /// </summary>
    public SyntaxToken? Offset { get; }

    /// <summary>
    /// The full target label name.
    /// </summary>
    public string TargetLabel =>
        ClassIdentifier.HasValue ? $"{ClassIdentifier.Value.Text}::{LabelIdentifier.Text}" : LabelIdentifier.Text;

    /// <summary>
    /// The frame offset value, or 0 if no offset is specified.
    /// </summary>
    public int FrameOffset => Offset.HasValue && Offset.Value.Value is long lv ? (int)lv : (Offset.HasValue && Offset.Value.Value is int iv ? iv : 0);

    public override SyntaxNodeKind Kind => SyntaxNodeKind.StateGoto;

    public StateGotoSyntax(
        SyntaxToken gotoKeyword,
        SyntaxToken? classIdentifier,
        SyntaxToken? colonColonToken,
        SyntaxToken labelIdentifier,
        SyntaxToken? plusToken,
        SyntaxToken? offset)
        : base(
            ComputeSpan(gotoKeyword.Span, ComputeLastSpan(labelIdentifier, plusToken, offset)),
            ComputeSpan(gotoKeyword.FullSpan, ComputeLastFullSpan(labelIdentifier, plusToken, offset)))
    {
        GotoKeyword = gotoKeyword;
        ClassIdentifier = classIdentifier;
        ColonColonToken = colonColonToken;
        LabelIdentifier = labelIdentifier;
        PlusToken = plusToken;
        Offset = offset;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield break;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return GotoKeyword;
        if (ClassIdentifier.HasValue) yield return ClassIdentifier.Value;
        if (ColonColonToken.HasValue) yield return ColonColonToken.Value;
        yield return LabelIdentifier;
        if (PlusToken.HasValue) yield return PlusToken.Value;
        if (Offset.HasValue) yield return Offset.Value;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitStateGoto(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitStateGoto(this);

    private static TextSpan ComputeLastSpan(SyntaxToken label, SyntaxToken? plus, SyntaxToken? offset)
    {
        if (offset.HasValue) return offset.Value.Span;
        if (plus.HasValue) return plus.Value.Span;
        return label.Span;
    }

    private static TextSpan ComputeLastFullSpan(SyntaxToken label, SyntaxToken? plus, SyntaxToken? offset)
    {
        if (offset.HasValue) return offset.Value.FullSpan;
        if (plus.HasValue) return plus.Value.FullSpan;
        return label.FullSpan;
    }

    private static TextSpan ComputeSpan(TextSpan first, TextSpan last)
    {
        return new TextSpan(first.Start, last.End - first.Start);
    }
}

/// <summary>
/// A <c>Stop</c> state directive that terminates the state sequence.
/// </summary>
public sealed class StateStopSyntax : StateSyntax
{
    /// <summary>
    /// The <c>Stop</c> keyword.
    /// </summary>
    public SyntaxToken StopKeyword { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.StateStop;

    public StateStopSyntax(SyntaxToken stopKeyword)
        : base(stopKeyword.Span, stopKeyword.FullSpan)
    {
        StopKeyword = stopKeyword;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield break;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return StopKeyword;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitStateStop(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitStateStop(this);
}

/// <summary>
/// A <c>Wait</c> state directive that holds the last frame indefinitely.
/// </summary>
public sealed class StateWaitSyntax : StateSyntax
{
    /// <summary>
    /// The <c>Wait</c> keyword.
    /// </summary>
    public SyntaxToken WaitKeyword { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.StateWait;

    public StateWaitSyntax(SyntaxToken waitKeyword)
        : base(waitKeyword.Span, waitKeyword.FullSpan)
    {
        WaitKeyword = waitKeyword;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield break;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return WaitKeyword;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitStateWait(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitStateWait(this);
}

/// <summary>
/// A <c>Loop</c> state directive that jumps back to the most recent label.
/// </summary>
public sealed class StateLoopSyntax : StateSyntax
{
    /// <summary>
    /// The <c>Loop</c> keyword.
    /// </summary>
    public SyntaxToken LoopKeyword { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.StateLoop;

    public StateLoopSyntax(SyntaxToken loopKeyword)
        : base(loopKeyword.Span, loopKeyword.FullSpan)
    {
        LoopKeyword = loopKeyword;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield break;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return LoopKeyword;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitStateLoop(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitStateLoop(this);
}

/// <summary>
/// A <c>Fail</c> state directive that signals failure to the calling code.
/// </summary>
public sealed class StateFailSyntax : StateSyntax
{
    /// <summary>
    /// The <c>Fail</c> keyword.
    /// </summary>
    public SyntaxToken FailKeyword { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.StateFail;

    public StateFailSyntax(SyntaxToken failKeyword)
        : base(failKeyword.Span, failKeyword.FullSpan)
    {
        FailKeyword = failKeyword;
    }

    public override IEnumerable<SyntaxNode> ChildNodes()
    {
        yield break;
    }

    public override IEnumerable<SyntaxToken> ChildTokens()
    {
        yield return FailKeyword;
    }

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitStateFail(this);

    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor) where TResult : default
        => visitor.VisitStateFail(this);
}
