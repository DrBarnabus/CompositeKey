namespace CompositeKey.Analyzers.Common.Tokenization;

public class TemplateStringTokenizer(char? primaryKeySeparator)
{
    private readonly char? _primaryKeySeparator = primaryKeySeparator;

    public TokenizeResult Tokenize(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty)
            return TokenizeResult.CreateFailure();

        var templateTokens = new List<TemplateToken>();

        int currentPosition = 0;
        while (currentPosition < input.Length)
        {
            char current = input[currentPosition];

            if (current == '{')
            {
                var result = ReadProperty(input, ref currentPosition);
                if (!result.Success)
                    return TokenizeResult.CreateFailure();

                templateTokens.Add(result.Value);
            }
            else if (char.IsLetterOrDigit(current))
            {
                var result = ReadConstantValue(input, ref currentPosition);
                if (!result.Success)
                    return TokenizeResult.CreateFailure();

                templateTokens.Add(result.Value);
            }
            else
            {
                if (_primaryKeySeparator == current)
                {
                    if (templateTokens.Any(tt => tt.Type == TemplateToken.TemplateTokenType.PrimaryDelimiter))
                        return TokenizeResult.CreateFailure();

                    templateTokens.Add(TemplateToken.PrimaryDelimiter(current));
                }
                else
                {
                    templateTokens.Add(TemplateToken.Delimiter(current));
                }

                currentPosition++;
            }
        }

        return TokenizeResult.CreateSuccess(templateTokens);
    }

    private static ReadResult<TemplateToken> ReadProperty(ReadOnlySpan<char> input, ref int currentPosition)
    {
        int startPosition = currentPosition + 1;
        currentPosition++;

        while (currentPosition < input.Length && input[currentPosition] != '}')
        {
            if (input[currentPosition] == '{')
                return ReadResult<TemplateToken>.CreateFailure();

            currentPosition++;
        }

        if (currentPosition >= input.Length || input[currentPosition] != '}')
            return ReadResult<TemplateToken>.CreateFailure();

        var propertySpan = input.Slice(startPosition, currentPosition - startPosition);
        currentPosition++;

        int colonIndex = propertySpan.IndexOf(':');
        var token = colonIndex != -1
            ? TemplateToken.Property(propertySpan[..colonIndex].ToString(), propertySpan[(colonIndex + 1)..].ToString())
            : TemplateToken.Property(propertySpan.ToString());

        return ReadResult<TemplateToken>.CreateSuccess(token);
    }

    private static ReadResult<TemplateToken> ReadConstantValue(ReadOnlySpan<char> input, ref int currentPosition)
    {
        int startPosition = currentPosition;

        while (currentPosition < input.Length && char.IsLetterOrDigit(input[currentPosition]))
            currentPosition++;

        var token = TemplateToken.Constant(input[startPosition..currentPosition].ToString());
        return ReadResult<TemplateToken>.CreateSuccess(token);
    }
}

public readonly record struct TokenizeResult(bool Success, List<TemplateToken> Tokens)
{
    public static TokenizeResult CreateFailure() => new(false, []);
    public static TokenizeResult CreateSuccess(List<TemplateToken> tokens) => new(true, tokens);
}

public readonly record struct ReadResult<T>(bool Success, T Value)
{
    public static ReadResult<T> CreateFailure() => new(false, default!);
    public static ReadResult<T> CreateSuccess(T value) => new(true, value);
}
