using System.Text;

namespace Infrastructure.Seed;

public sealed record CsvDocument(IReadOnlyList<string> Headers, IReadOnlyList<CsvRow> Rows);

public sealed record CsvRow(int LineNumber, IReadOnlyDictionary<string, string> Values)
{
    public string this[string header] => Values[header];
}

public sealed class CsvFormatException(string message, int lineNumber)
    : FormatException($"CSV inválido na linha {lineNumber}: {message}")
{
    public int LineNumber { get; } = lineNumber;
}

public static class StrictCsvParser
{
    public static CsvDocument Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var records = ReadRecords(content);
        if (records.Count == 0)
            throw new CsvFormatException("arquivo sem header.", 1);

        var headers = records[0].Fields.ToArray();
        if (headers.Length > 0)
            headers[0] = headers[0].TrimStart('\uFEFF');
        if (headers.Any(string.IsNullOrWhiteSpace))
            throw new CsvFormatException("header vazio.", records[0].Line);
        if (headers.Distinct(StringComparer.Ordinal).Count() != headers.Length)
            throw new CsvFormatException("header duplicado.", records[0].Line);

        var rows = new List<CsvRow>(Math.Max(0, records.Count - 1));
        foreach (var record in records.Skip(1))
        {
            if (record.Fields.Count != headers.Length)
                throw new CsvFormatException(
                    $"esperadas {headers.Length} colunas, recebidas {record.Fields.Count}.",
                    record.Line);

            rows.Add(new CsvRow(
                record.Line,
                headers.Zip(record.Fields, (header, value) => (header, value))
                    .ToDictionary(x => x.header, x => x.value, StringComparer.Ordinal)));
        }

        return new CsvDocument(headers, rows);
    }

    public static async Task<CsvDocument> ParseFileAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        using var reader = new StreamReader(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
            detectEncodingFromByteOrderMarks: true);
        return Parse(await reader.ReadToEndAsync(cancellationToken));
    }

    private static List<Record> ReadRecords(string content)
    {
        var records = new List<Record>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var line = 1;
        var recordLine = 1;
        var quoted = false;
        var closedQuote = false;
        var fieldStarted = false;

        for (var index = 0; index < content.Length; index++)
        {
            var ch = content[index];
            if (quoted)
            {
                if (ch == '"')
                {
                    if (index + 1 < content.Length && content[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        quoted = false;
                        closedQuote = true;
                    }
                }
                else
                {
                    field.Append(ch);
                    if (ch == '\n')
                        line++;
                    else if (ch == '\r' && (index + 1 >= content.Length || content[index + 1] != '\n'))
                        line++;
                }

                continue;
            }

            if (closedQuote && ch is not (',' or '\r' or '\n'))
                throw new CsvFormatException("caractere após fechamento de aspas.", line);

            if (ch == '"')
            {
                if (fieldStarted)
                    throw new CsvFormatException("aspas em campo não delimitado.", line);
                quoted = true;
                fieldStarted = true;
            }
            else if (ch == ',')
            {
                fields.Add(field.ToString());
                field.Clear();
                fieldStarted = false;
                closedQuote = false;
            }
            else if (ch is '\r' or '\n')
            {
                if (ch == '\r' && index + 1 < content.Length && content[index + 1] == '\n')
                    index++;
                fields.Add(field.ToString());
                records.Add(new Record(recordLine, fields.ToArray()));
                fields.Clear();
                field.Clear();
                fieldStarted = false;
                closedQuote = false;
                line++;
                recordLine = line;
            }
            else
            {
                field.Append(ch);
                fieldStarted = true;
            }
        }

        if (quoted)
            throw new CsvFormatException("campo delimitado sem aspas de fechamento.", recordLine);

        if (fieldStarted || closedQuote || fields.Count > 0)
        {
            fields.Add(field.ToString());
            records.Add(new Record(recordLine, fields.ToArray()));
        }

        return records;
    }

    private sealed record Record(int Line, IReadOnlyList<string> Fields);
}
