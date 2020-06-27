# jaytwo.DataExport.Csv

[![NuGet version (jaytwo.DataExport.Csv)](https://img.shields.io/nuget/v/jaytwo.DataExport.Csv.svg?style=flat-square)](https://www.nuget.org/packages/jaytwo.DataExport.Csv/)

Making it slightly easier to write a quick CSV.

## Installation

Add the NuGet package

```
PM> Install-Package jaytwo.DataExport.Csv
```

## Usage

```csharp
var csv = new CsvWriter(textWriter);
await csv.WriteAsync(new[]
{
    new { a = "hello", b = "world" },
    new { a = "foo", b = "bar" },
    new { a = "fizz", b = "buzz" },
});
```

---

Made with &hearts; by Jake
