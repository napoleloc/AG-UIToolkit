using System;

namespace EncosyTower.Unions.SourceGen
{
    /// <summary>
    /// An attribute that indicates that a given class is generated by the Generic Union generator.
    /// </summary>
    /// <remarks>
    /// This attribute is not intended to be used directly by user code to annotate user-defined types.
    /// <br/>
    /// However, it can be used in other contexts, such as reflection.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class GeneratedGenericUnionsAttribute : Attribute { }
}
