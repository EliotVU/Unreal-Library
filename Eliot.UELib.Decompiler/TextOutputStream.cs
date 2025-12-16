using System.Runtime.CompilerServices;
using System.Text;

namespace UELib.Decompiler;

public class TextOutputStream(TextWriter innerWriter) : TextWriter(innerWriter.FormatProvider)
{
    protected const string IndentString = "    ";
    protected int Indent;

    public override Encoding Encoding { get; } = Encoding.UTF8;

    public override void Write(char value) => innerWriter.Write(value);

    public virtual void WriteIndent()
    {
        for (int i = 0; i < Indent; i++)
        {
            Write(IndentString);
        }
    }

    public override void WriteLine()
    {
        base.WriteLine();
        WriteIndent();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSpace() => Write(' ');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDot() => Write('.');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteComma() => Write(',');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAssignment() => Write('=');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSingleQuote() => Write('\'');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDoubleQuote() => Write('"');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteColon() => Write(':');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSemicolon() => Write(';');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteToken(string token) => Write(token);

    public void WriteEscaped(string input)
    {
        foreach (char c in input)
        {
            switch (c)
            {
                case '\"':
                    Write("\\\"");
                    break;
                case '\\':
                    Write(@"\\");
                    break;
                case '\n':
                    Write(@"\n");
                    break;
                case '\r':
                    Write(@"\r");
                    break;
                default:
                    Write(c);
                    break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSafe(string value)
    {
        try
        {
            Write(value);
        }
        catch (Exception ex)
        {
            WriteException(ex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSafe(Action action)
    {
        try
        {
            action.Invoke();
        }
        catch (Exception ex)
        {
            WriteException(ex);
        }
    }

    /// <summary>
    ///     Writes a block of braces around the output of the action.
    ///     The action's output will be indented.
    ///     It is the responsibility for the action to output new lines before and after any brace.
    ///     <param name="action">An action to be invoked after the writing of '{' and ending before the '}'</param>
    /// </summary>
    public void WriteBlock(Action action, bool newLines = true)
    {
        if (newLines)
        {
            WriteLine();
        }

        Write('{');

        try
        {
            WriteIndented(action);
        }
        finally
        {
            if (newLines)
            {
                WriteLine();
            }

            Write('}');
        }
    }

    /// <summary>
    ///     Indents all output of the action by one tab (4 spaces)
    /// </summary>
    /// <param name="action">
    ///     An action to be invoked after the new applied indentation, the indentation will resume after the
    ///     action.
    /// </param>
    public void WriteIndented(Action action)
    {
        ++Indent;
        try
        {
            action.Invoke();
        }
        finally
        {
            --Indent;
        }
    }

    public void WriteAligned(int padding)
    {
        for (int i = 0; i < padding; i++)
        {
            Write(' ');
        }
    }

    public virtual void WriteColumnAligned(int padding)
    {
        for (int i = 0; i < padding; i++)
        {
            Write(' ');
        }
    }

    public virtual void WriteComment(string comment)
    {
        Write("//");
        Write(comment);
    }

    public virtual void WriteCommentBlock(string comment)
    {
        Write("/*");
        Write(comment);
        Write("*/");
    }

    public virtual void WriteException(Exception exception)
    {
        WriteCommentBlock(exception.ToString());
    }

    public enum KeywordType
    {
        Default,
        Control,
    }

    public virtual void WriteKeyword(string keyword, KeywordType type = KeywordType.Default) => Write(keyword);

    public virtual void WriteReference(object? reference, string identifier, object? source = null) =>
        Write(identifier);

    public virtual void WriteBegin(object subject, string name) => WriteComment(name);

    public virtual void WriteEnd(object subject, string name) => WriteComment(name);
}
