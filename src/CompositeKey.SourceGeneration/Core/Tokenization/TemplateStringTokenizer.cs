﻿namespace CompositeKey.SourceGeneration.Core.Tokenization;

public class TemplateStringTokenizer(char? primaryKeySeparator)
{
    private readonly char? _primaryKeySeparator = primaryKeySeparator;

    public List<TemplateToken> Tokenize(ReadOnlySpan<char> input)
    {
        try
        {
            var templateTokens = new List<TemplateToken>();

            int currentPosition = 0;
            while (currentPosition < input.Length)
            {
                char current = input[currentPosition];

                if (current == '{')
                {
                    templateTokens.Add(ReadProperty(input, ref currentPosition));
                }
                else if (char.IsLetterOrDigit(current))
                {
                    templateTokens.Add(ReadConstantValue(input, ref currentPosition));
                }
                else
                {
                    var templateToken = _primaryKeySeparator == current
                        ? TemplateToken.PrimaryDelimiter(current)
                        : TemplateToken.Delimiter(current);

                    templateTokens.Add(templateToken);
                    currentPosition++;
                }
            }

            return templateTokens;
        }
        catch
        {
            return [];
        }
    }

    private static TemplateToken ReadProperty(ReadOnlySpan<char> input, ref int currentPosition)
    {
        int startPosition = currentPosition + 1;
        currentPosition++;

        while (currentPosition < input.Length && input[currentPosition] != '}')
        {
            if (input[currentPosition] == '{')
                throw new InvalidOperationException("Encountered a '{{' character before a closing '}}' when parsing a property.");

            currentPosition++;
        }

        if (input[currentPosition] != '}')
            throw new InvalidOperationException("Finished parsing property but last character was not a '}}'.");

        input = input.Slice(startPosition, currentPosition - startPosition);
        currentPosition++;

        int colonIndex = input.IndexOf(':');
        return colonIndex != -1
            ? TemplateToken.Property(input[..colonIndex].ToString(), input[(colonIndex + 1)..].ToString())
            : TemplateToken.Property(input.ToString());
    }

    private static TemplateToken ReadConstantValue(ReadOnlySpan<char> input, ref int currentPosition)
    {
        int startPosition = currentPosition;

        while (currentPosition < input.Length && char.IsLetterOrDigit(input[currentPosition]))
            currentPosition++;

        return TemplateToken.Constant(input[startPosition..currentPosition].ToString());
    }
}
