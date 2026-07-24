namespace Brontide.Reference.Conformance;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SpecSectionAttribute(string section) : Attribute
{
    public string Section { get; } = section;
}
