﻿namespace CompositeKey.SourceGeneration.Model.Key;

public sealed record PrimaryDelimiterKeyPart(char Value) : DelimiterKeyPart(Value);
