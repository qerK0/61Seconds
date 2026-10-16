using System;

namespace SixtyOne.Text
{
    /// <summary>
    /// Marks a static class whose const string fields are text keys used from code.
    /// The text validator counts them as referenced.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TextKeyConstantsAttribute : Attribute
    {
    }
}
