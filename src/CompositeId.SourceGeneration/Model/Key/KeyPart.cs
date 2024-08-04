namespace CompositeId.SourceGeneration.Model.Key;

public abstract record KeyPart
{
    public required int LengthRequired { get; init; }

    public bool ExactLengthRequirement { get; init; } = true;
}
