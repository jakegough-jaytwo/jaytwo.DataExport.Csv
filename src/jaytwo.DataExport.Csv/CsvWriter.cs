using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.DataExport.Csv;

public class CsvWriter : IAsyncDisposable, IDisposable
{
    private readonly TextWriter _textWriter;
    private readonly bool _disposeTextWriter;

    private bool _writeStarted = false;
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1);

    public CsvWriter(TextWriter textWriter)
        : this(textWriter, true)
    {
    }

    public CsvWriter(TextWriter textWriter, bool disposeTextWriter)
    {
        _textWriter = textWriter;
        _disposeTextWriter = disposeTextWriter;
    }

    public bool IncludeHeader { get; set; } = true;

    public static CsvWriter Create(StringBuilder stringBuilder)
    {
        var writer = new StringWriter(stringBuilder);
        return new CsvWriter(writer, disposeTextWriter: true);
    }

    public static CsvWriter Create(Stream stream)
    {
        var writer = new StreamWriter(stream);
        return new CsvWriter(writer, disposeTextWriter: true);
    }

    public Task WriteAsync(IDictionary[] rows, CancellationToken cancellationToken = default)
        => WriteAsync(rows as IEnumerable<IDictionary>, cancellationToken);

    public async Task WriteAsync(IEnumerable<IDictionary> rows, CancellationToken cancellationToken = default)
        => await WriteAsync<IDictionary>(ToAsyncEnumerable(rows), cancellationToken);

    public async Task WriteAsync(IAsyncEnumerable<IDictionary> rows, CancellationToken cancellationToken = default)
        => await WriteAsync<IDictionary>(rows, cancellationToken);

    public Task WriteAsync<T>(T[] rows, CancellationToken cancellationToken = default)
        => WriteAsync(rows as IEnumerable<T>, cancellationToken);

    public async Task WriteAsync<T>(IEnumerable<T> rows, CancellationToken cancellationToken = default)
        => await WriteAsync(ToAsyncEnumerable(rows), cancellationToken);

    public async Task WriteAsync<T>(IAsyncEnumerable<T> rows, CancellationToken cancellationToken = default)
    {
        await foreach (var row in rows.WithCancellation(cancellationToken))
        {
            await WriteAsync<T>(row, cancellationToken);
        }

        await FlushAsync();
    }

    public async Task WriteAsync<T>(T row, CancellationToken cancellationToken = default)
    {
        await _semaphore.RunAsync(async () =>
        {
            if (!_writeStarted)
            {
                if (IncludeHeader)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await WriteHeaderWithoutSemaphoreAsync<T>(row);
                }

                _writeStarted = true;
            }
        });

        var line = GetCsvRow(row);
        if (line != null)
        {
            await _semaphore.RunAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _textWriter.WriteLineAsync(line);
            });
        }
    }

    public async Task WriteHeaderAsync<T>(T row)
    {
        await _semaphore.RunAsync(async () =>
        {
            await WriteHeaderWithoutSemaphoreAsync<T>(row);
        });
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.RunAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _textWriter.FlushAsync();
        });
    }

    public void Dispose()
    {
        if (_disposeTextWriter)
        {
            _semaphore.Run(() =>
            {
                _textWriter.Dispose();
            });
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposeTextWriter)
        {
            await _semaphore.RunAsync(async () =>
            {
                await _textWriter.DisposeAsync();
            });
        }
    }

    protected internal virtual bool ShouldQuote(string? value)
    {
        var result = (value != null)
            && (value.Contains(',')
                || value.Contains('"')
                || value.Contains('\r')
                || value.Contains('\n'));

        return result;
    }

    protected internal virtual string? Quote(string? value)
    {
        var result = '"' + value?.Replace("\"", "\"\"") + '"';
        return result;
    }

    protected internal IDictionary? ToDictionary(object? row)
    {
        if (row == null)
        {
            return null;
        }
        else if (row is IDictionary dict)
        {
            return dict;
        }

        var asDictionary = row.GetType()
            .GetRuntimeProperties()
            .ToDictionary(
                x => x.Name,
                x => GetValueAsString(x.GetValue(row)));

        return asDictionary;
    }

    protected internal virtual string? GetValueAsString(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return value switch
        {
            string str => str,
            DateTimeOffset x => x.ToString("o", CultureInfo.InvariantCulture),
            DateTime x => x.ToString("o", CultureInfo.InvariantCulture),
            TimeSpan x => x.ToString("c", CultureInfo.InvariantCulture),
            Enum x => x.ToString("G"),
            Guid x => x.ToString("D"),
            bool x => x.ToString(CultureInfo.InvariantCulture),
            byte x => x.ToString(CultureInfo.InvariantCulture),
            sbyte x => x.ToString(CultureInfo.InvariantCulture),
            int x => x.ToString(CultureInfo.InvariantCulture),
            uint x => x.ToString(CultureInfo.InvariantCulture),
            short x => x.ToString(CultureInfo.InvariantCulture),
            ushort x => x.ToString(CultureInfo.InvariantCulture),
            long x => x.ToString(CultureInfo.InvariantCulture),
            ulong x => x.ToString(CultureInfo.InvariantCulture),
            float x => x.ToString(CultureInfo.InvariantCulture),
            double x => x.ToString(CultureInfo.InvariantCulture),
            decimal x => x.ToString(CultureInfo.InvariantCulture),
            byte[] x => Convert.ToBase64String(x),
            IFormattable x => x.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return item;
            await Task.Yield(); // ensures it's really async
        }
    }

    private async Task WriteHeaderWithoutSemaphoreAsync<T>(T row)
    {
        var line = GetCsvHeader(row);
        await _textWriter.WriteLineAsync(line);
    }

    private string? GetCsvHeader(object? row)
    {
        return row switch
        {
            null => null,
            IDictionary x => GetCsvHeader(x),
            _ => GetCsvHeader(ToDictionary(row)),
        };
    }

    private string? GetCsvHeader(IDictionary? row)
    {
        if (row == null)
        {
            return null;
        }

        var values = row.Keys.Cast<object>()
            .Select(x => GetValueAsString(x)!)
            .ToList();

        return GetCsvLineString(values);
    }

    private string? GetCsvRow(object? row)
    {
        return row switch
        {
            null => null,
            IDictionary x => GetCsvRow(x),
            _ => GetCsvRow(ToDictionary(row)),
        };
    }

    private string? GetCsvRow(IDictionary? row)
    {
        if (row == null)
        {
            return null;
        }

        var values = row.Values.Cast<object?>()
            .Select(x => GetValueAsString(x)!)
            .ToList();

        return GetCsvLineString(values);
    }

    private string GetCsvLineString(IList<string> values)
    {
        var escapedValues = values
            .Select(x => ShouldQuote(x) ? Quote(x) : x)
            .ToArray();

        var result = string.Join(",", escapedValues);
        return result;
    }
}
