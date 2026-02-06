namespace WAD.NET.ZScript.Syntax;

/// <summary>
/// Defines all token kinds recognized by the ZScript/DECORATE lexer.
/// </summary>
public enum SyntaxTokenKind
{
    None,
    EndOfFile,

    // Literals
    IntegerLiteral,
    FloatLiteral,
    StringLiteral,
    NameLiteral,

    // Identifiers
    Identifier,

    // Keywords - Shared
    ClassKeyword,
    StructKeyword,
    EnumKeyword,
    ConstKeyword,
    StaticKeyword,
    PrivateKeyword,
    ProtectedKeyword,
    VirtualKeyword,
    OverrideKeyword,
    FinalKeyword,
    NativeKeyword,
    DefaultKeyword,
    StatesKeyword,

    // Keywords - DECORATE specific
    ActorKeyword,
    ReplacesKeyword,

    // Keywords - ZScript specific
    VersionKeyword,
    ExtendKeyword,
    MixinKeyword,
    AbstractKeyword,
    DeprecatedKeyword,
    ReadOnlyKeyword,
    LetKeyword,
    OutKeyword,
    InKeyword,

    // Control flow
    IfKeyword,
    ElseKeyword,
    WhileKeyword,
    DoKeyword,
    ForKeyword,
    ForEachKeyword,
    SwitchKeyword,
    CaseKeyword,
    BreakKeyword,
    ContinueKeyword,
    ReturnKeyword,
    GotoKeyword,

    // Types
    VoidKeyword,
    IntKeyword,
    UIntKeyword,
    FloatKeyword,
    DoubleKeyword,
    BoolKeyword,
    StringKeyword,
    VectorKeyword,
    NameKeyword,
    StateKeyword,
    ColorKeyword,
    SoundKeyword,
    ArrayKeyword,
    MapKeyword,

    // Boolean literals
    TrueKeyword,
    FalseKeyword,
    NullKeyword,

    // State-specific
    StopKeyword,
    WaitKeyword,
    FailKeyword,
    LoopKeyword,

    // Operators
    Plus,
    Minus,
    Asterisk,
    Slash,
    Percent,
    Ampersand,
    Pipe,
    Caret,
    Tilde,
    Exclamation,
    Question,

    // Comparison
    EqualsEquals,
    ExclamationEquals,
    LessThan,
    GreaterThan,
    LessThanEquals,
    GreaterThanEquals,
    ApproxEquals,

    // Logical
    AmpersandAmpersand,
    PipePipe,

    // Assignment
    Equals,
    PlusEquals,
    MinusEquals,
    AsteriskEquals,
    SlashEquals,
    PercentEquals,
    AmpersandEquals,
    PipeEquals,
    CaretEquals,

    // Increment/Decrement
    PlusPlus,
    MinusMinus,

    // Shift
    LessThanLessThan,
    GreaterThanGreaterThan,
    GreaterThanGreaterThanGreaterThan,

    // Punctuation
    OpenParen,
    CloseParen,
    OpenBrace,
    CloseBrace,
    OpenBracket,
    CloseBracket,
    Semicolon,
    Colon,
    ColonColon,
    Comma,
    Dot,
    DotDot,
    Arrow,

    // Preprocessor
    Hash,

    // Special
    BadToken
}
