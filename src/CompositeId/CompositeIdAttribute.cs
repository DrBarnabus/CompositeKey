namespace CompositeId;

[AttributeUsage(AttributeTargets.Class)]
#if BUILDING_SOURCE_GENERATOR
internal
#else
public
#endif
sealed class CompositeIdAttribute(string template) : Attribute
{
    public string Template { get; } = template;

    public char PrimaryKeySeparator { get; set; }
}
