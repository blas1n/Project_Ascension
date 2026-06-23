#if UNITY_5_3_OR_NEWER
namespace System.Runtime.CompilerServices
{
    // Polyfill so C# record `init` accessors compile under Unity's .NET Standard
    // profile, which does not define this type. The .NET server build already
    // provides it, so this is excluded from non-Unity compilation.
    internal static class IsExternalInit { }
}
#endif
