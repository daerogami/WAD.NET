// This polyfill allows use of 'init' property accessors in .NET Standard 2.1
// See: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init

#if NETSTANDARD2_0 || NETSTANDARD2_1

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}

#endif
