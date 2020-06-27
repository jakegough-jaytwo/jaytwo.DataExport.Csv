using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LumenWorks.Framework.IO.Csv;
using Xunit;

namespace jaytwo.DataExport.Csv.Tests
{
    public class CsvWriterTests
    {
        [Fact]
        public async Task WriteAsync_works()
        {
            // arrange
            var stringBuilder = new StringBuilder();
            var csv = CsvWriter.Create(stringBuilder);

            var rows = new[]
            {
                new { a = "hello", b = "world" },
                new { a = "foo", b = "bar" },
                new { a = "fizz", b = "buzz" },
                new { a = "with \"quotes\"", b = "with\rreturns\nand newlines" },
                new { a = string.Empty, b = "and empty string" },
            };

            // act
            await csv.WriteAsync(rows);

            // assert
            Assert.NotEmpty(stringBuilder.ToString());

            int i = 0;
            using (var reader = new StringReader(stringBuilder.ToString()))
            using (var csvReader = new CsvReader(reader, true))
            {
                Assert.All(csvReader, currentCsvRow =>
                {
                    Assert.Equal(rows[i].a, currentCsvRow[0]);
                    Assert.Equal(rows[i].b, currentCsvRow[1]);
                    i++;
                });
            }

            Assert.Equal(rows.Length, i);
        }

        [Fact]
        public async Task WriteAsync_without_header_works()
        {
            // arrange
            var stringBuilder = new StringBuilder();
            var csv = CsvWriter.Create(stringBuilder);
            csv.IncludeHeader = false;

            var rows = new[]
            {
                new { a = "hello", b = "world" },
                new { a = "foo", b = "bar" },
                new { a = "fizz", b = "buzz" },
                new { a = "with \"quotes\"", b = "with\rreturns\nand newlines" },
                new { a = string.Empty, b = "and empty string" },
            };

            // act
            await csv.WriteAsync(rows);

            // assert
            Assert.NotEmpty(stringBuilder.ToString());

            int i = 0;
            using (var reader = new StringReader(stringBuilder.ToString()))
            using (var csvReader = new CsvReader(reader, false))
            {
                Assert.All(csvReader, currentCsvRow =>
                {
                    Assert.Equal(rows[i].a, currentCsvRow[0]);
                    Assert.Equal(rows[i].b, currentCsvRow[1]);
                    i++;
                });
            }

            Assert.Equal(rows.Length, i);
        }

        [Fact]
        public void Write_works()
        {
            // arrange
            var stringBuilder = new StringBuilder();
            var csv = CsvWriter.Create(stringBuilder);

            var rows = new[]
            {
                new { a = "hello", b = "world" },
                new { a = "foo", b = "bar" },
                new { a = "fizz", b = "buzz" },
                new { a = "with \"quotes\"", b = "with\rreturns\nand newlines" },
                new { a = string.Empty, b = "and empty string" },
            };

            // act
            csv.Write(rows);

            // assert
            Assert.NotEmpty(stringBuilder.ToString());

            int i = 0;
            using (var reader = new StringReader(stringBuilder.ToString()))
            using (var csvReader = new CsvReader(reader, true))
            {
                Assert.All(csvReader, currentCsvRow =>
                {
                    Assert.Equal(rows[i].a, currentCsvRow[0]);
                    Assert.Equal(rows[i].b, currentCsvRow[1]);
                    i++;
                });
            }

            Assert.Equal(rows.Length, i);
        }

        [Fact]
        public void Write_without_header_works()
        {
            // arrange
            var stringBuilder = new StringBuilder();
            var csv = CsvWriter.Create(stringBuilder);
            csv.IncludeHeader = false;

            var rows = new[]
            {
                new { a = "hello", b = "world" },
                new { a = "foo", b = "bar" },
                new { a = "fizz", b = "buzz" },
                new { a = "with \"quotes\"", b = "with\rreturns\nand newlines" },
                new { a = string.Empty, b = "and empty string" },
            };

            // act
            csv.Write(rows);

            // assert
            Assert.NotEmpty(stringBuilder.ToString());

            int i = 0;
            using (var reader = new StringReader(stringBuilder.ToString()))
            using (var csvReader = new CsvReader(reader, false))
            {
                Assert.All(csvReader, currentCsvRow =>
                {
                    Assert.Equal(rows[i].a, currentCsvRow[0]);
                    Assert.Equal(rows[i].b, currentCsvRow[1]);
                    i++;
                });
            }

            Assert.Equal(rows.Length, i);
        }
    }
}
