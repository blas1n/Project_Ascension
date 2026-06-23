#if UNITY_5_3_OR_NEWER
// Unity-only no-op stand-ins for the MessagePack attributes. They let the
// Contracts package compile in the Unity client without pulling in the full
// MessagePack-CSharp package (which needs IL2CPP/AOT code generation). The
// server builds without UNITY_5_3_OR_NEWER and uses the real MessagePack
// attributes for serialization.
//
// When ENet networking is added to the client, replace this shim with the real
// MessagePack-CSharp Unity package and delete this file.
namespace MessagePack
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    internal sealed class MessagePackObjectAttribute : System.Attribute
    {
        public MessagePackObjectAttribute(bool keyAsPropertyName = false) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field | System.AttributeTargets.Parameter)]
    internal sealed class KeyAttribute : System.Attribute
    {
        public KeyAttribute(int index) { }
        public KeyAttribute(string name) { }
    }
}
#endif
