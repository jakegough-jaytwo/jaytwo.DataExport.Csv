using System;
using System.Text;
using System.Threading.Tasks;
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
            };

            // act
            await csv.WriteAsync(rows);

            // assert
            Assert.NotEmpty(stringBuilder.ToString());
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
            };

            // act
            csv.Write(rows);

            // assert
            Assert.NotEmpty(stringBuilder.ToString());
        }
    }
}
