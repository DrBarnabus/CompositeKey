using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace CompositeId.SourceGeneration.Core;

internal sealed class SourceWriter
{
    private readonly StringBuilder _stringBuilder = new();

    private int _indentation;

    public int Indentation
    {
        get => _indentation;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _indentation = value;
        }
    }

    public void WriteLine() => _stringBuilder.AppendLine();

    public void WriteLine(string text)
    {
        AddIndentation();
        _stringBuilder.AppendLine(text);
    }

    public void WriteLines(string text)
    {
        if (_indentation == 0)
        {
            _stringBuilder.AppendLine(text);
            return;
        }

        bool isFinalLine;
        var remainingText = text.AsSpan();

        do
        {
            var nextLine = GetNextLine(ref remainingText, out isFinalLine);

            AddIndentation();
            _stringBuilder.AppendLine(nextLine.ToString());
        } while (!isFinalLine);

        static ReadOnlySpan<char> GetNextLine(ref ReadOnlySpan<char> remainingText, out bool isFinalLine)
        {
            if (remainingText.IsEmpty)
            {
                isFinalLine = true;
                return default;
            }

            ReadOnlySpan<char> restOfText;

            int lineLength = remainingText.IndexOf('\n');
            if (lineLength == -1)
            {
                isFinalLine = true;
                lineLength = remainingText.Length;
                restOfText = default;
            }
            else
            {
                isFinalLine = false;
                restOfText = remainingText[(lineLength + 1)..];
            }

            if ((uint)lineLength > 0 && remainingText[lineLength - 1] == '\r')
            {
                lineLength--;
            }

            var next = remainingText[..lineLength];
            remainingText = restOfText;

            return next;
        }
    }

    public SourceText ToSourceText()
    {
        Debug.Assert(_indentation == 0 && _stringBuilder.Length > 0);
        return SourceText.From(_stringBuilder.ToString(), Encoding.UTF8);
    }

    private void AddIndentation() => _stringBuilder.Append(' ', _indentation * 4);
}
