using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.DataExport.Csv
{
    public class CsvWriter : IDisposable
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
            return new CsvWriter(writer);
        }

        public static CsvWriter Create(Stream stream)
        {
            var writer = new StreamWriter(stream);
            return new CsvWriter(writer);
        }

#if !NETSTANDARD1_1
        public static CsvWriter Create(string fileName)
        {
            var writer = File.CreateText(fileName);
            return new CsvWriter(writer);
        }
#endif

#if NETSTANDARD2
        public Task WriteAsync<T>(IAsyncEnumerable<T> rows) => WriteAsync<T>(rows, CancellationToken.None);

        public async Task WriteAsync<T>(IAsyncEnumerable<T> rows, CancellationToken cancellationToken)
        {
            await _semaphore.RunAsync(async () =>
            {
                if (!_writeStarted)
                {
                    if (IncludeHeader)
                    {
                        await WriteHeaderWithoutSemaphoreAsync<T>();
                    }

                    _writeStarted = true;
                }
            });

            await foreach (var row in rows.WithCancellation(cancellationToken)) // await foreach is a C# 8.0 language feature
            {
                var line = GetCsvRow(row);

                await _semaphore.RunAsync(async () =>
                {
                    await _textWriter.WriteLineAsync(line);
                });
            }

            await _semaphore.RunAsync(async () =>
            {
                await _textWriter.FlushAsync();
            });
        }
#endif

        public async Task WriteAsync<T>(IEnumerable<T> rows)
        {
            await _semaphore.RunAsync(async () =>
            {
                if (!_writeStarted)
                {
                    if (IncludeHeader)
                    {
                        await WriteHeaderWithoutSemaphoreAsync<T>();
                    }

                    _writeStarted = true;
                }

                foreach (var row in rows)
                {
                    var line = GetCsvRow(row);
                    await _textWriter.WriteLineAsync(line);
                }

                await _textWriter.FlushAsync();
            });
        }

        public Task WriteAsync<T>(params T[] rows)
        {
            return WriteAsync(rows as IEnumerable<T>);
        }

        public Task WriteHeaderAsync<T>(T anonymousObjectPrototype)
        {
            return WriteHeaderAsync<T>();
        }

        public async Task WriteHeaderAsync<T>()
        {
            await _semaphore.RunAsync(async () =>
            {
                await WriteHeaderWithoutSemaphoreAsync<T>();
            });
        }

        public void WriteHeader<T>(T anonymousObjectPrototype)
        {
            WriteHeader<T>();
        }

        public void WriteHeader<T>()
        {
            _semaphore.Run(() =>
            {
                WriteHeaderWithoutSemaphore<T>();
            });
        }

        public void Write<T>(IEnumerable<T> rows)
        {
            _semaphore.Run(() =>
            {
                if (!_writeStarted)
                {
                    if (IncludeHeader)
                    {
                        WriteHeaderWithoutSemaphore<T>();
                    }

                    _writeStarted = true;
                }

                foreach (var row in rows)
                {
                    var line = GetCsvRow(row);
                    _textWriter.WriteLine(line);
                }

                _textWriter.Flush();
            });
        }

        public void Write<T>(params T[] rows)
        {
            Write(rows as IEnumerable<T>);
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

        protected internal virtual bool ShouldEQuote(string value)
        {
            var result = (value != null)
                && (value.Contains(",")
                    || value.Contains("\"")
                    || value.Contains("\r")
                    || value.Contains("\n"));

            return result;
        }

        protected internal virtual string Quote(string value)
        {
            var result = "\"" + value.Replace("\"", "\"\"") + "\"";
            return result;
        }

        protected internal virtual string GetValueAsString(object value)
        {
            if (value is null)
            {
                return null;
            }
            else if (value is DateTimeOffset)
            {
                return GetValueAsString((DateTimeOffset)value);
            }
            else if (value is DateTime)
            {
                return GetValueAsString((DateTime)value);
            }
            else if (value is byte[])
            {
                return GetValueAsString((byte[])value);
            }
            else
            {
                return value.ToString();
            }
        }

        protected internal virtual string GetValueAsString(DateTimeOffset value)
        {
            return value.ToString("o"); // "o" is the 'RoundTrip' fomrat specifier
        }

        protected internal virtual string GetValueAsString(DateTime value)
        {
            return value.ToString("o"); // "o" is the 'RoundTrip' fomrat specifier
        }

        protected internal virtual string GetValueAsString(byte[] value)
        {
            return Convert.ToBase64String(value);
        }

        private void WriteHeaderWithoutSemaphore<T>()
        {
            var line = GetCsvHeader<T>();
            _textWriter.WriteLine(line);
        }

        private async Task WriteHeaderWithoutSemaphoreAsync<T>()
        {
            var line = GetCsvHeader<T>();
            await _textWriter.WriteLineAsync(line);
        }

        private string GetCsvHeader<T>()
        {
            var propertyNames = typeof(T).GetRuntimeProperties()
                .Select(m => m.Name)
                .ToList();

            return GetCsvRowString(propertyNames);
        }

        private string GetCsvRow<T>(T obj)
        {
            var propertyValues = obj.GetType().GetRuntimeProperties()
                .Select(x => GetValueAsString(x.GetValue(obj)))
                .ToList();

            return GetCsvRowString(propertyValues);
        }

        private string GetCsvRowString(IList<string> values)
        {
            var escapedValues = values
                .Select(x => ShouldEQuote(x) ? Quote(x) : x)
                .ToArray();

            var result = string.Join(",", escapedValues);
            return result;
        }
    }
}
