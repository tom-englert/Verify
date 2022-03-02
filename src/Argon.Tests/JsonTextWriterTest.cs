// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;

namespace Argon.Tests;

public class JsonTextWriterTest : TestFixtureBase
{
    [Fact]
    public void BufferTest()
    {
        var arrayPool = new FakeArrayPool();

        var longString = new string('A', 2000);
        var longEscapedString = $"Hello!{new string('!', 50)}{new string('\n', 1000)}Good bye!";
        var longerEscapedString = $"Hello!{new string('!', 2000)}{new string('\n', 1000)}Good bye!";

        for (var i = 0; i < 1000; i++)
        {
            var stringWriter = new StringWriter(CultureInfo.InvariantCulture);

            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                jsonWriter.ArrayPool = arrayPool;

                jsonWriter.WriteStartObject();

                jsonWriter.WritePropertyName("Prop1");
                jsonWriter.WriteValue(new DateTime(2000, 12, 12, 12, 12, 12, DateTimeKind.Utc));

                jsonWriter.WritePropertyName("Prop2");
                jsonWriter.WriteValue(longString);

                jsonWriter.WritePropertyName("Prop3");
                jsonWriter.WriteValue(longEscapedString);

                jsonWriter.WritePropertyName("Prop4");
                jsonWriter.WriteValue(longerEscapedString);

                jsonWriter.WriteEndObject();
            }

            if ((i + 1) % 100 == 0)
            {
                Console.WriteLine($"Allocated buffers: {arrayPool.FreeArrays.Count}");
            }
        }

        Assert.Equal(0, arrayPool.UsedArrays.Count);
        Assert.Equal(3, arrayPool.FreeArrays.Count);
    }

    [Fact]
    public void BufferTest_WithError()
    {
        var arrayPool = new FakeArrayPool();

        var stringWriter = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            // dispose will free used buffers
            using (var writer = new JsonTextWriter(stringWriter))
            {
                writer.ArrayPool = arrayPool;

                writer.WriteStartObject();

                writer.WritePropertyName("Prop1");
                writer.WriteValue(new DateTime(2000, 12, 12, 12, 12, 12, DateTimeKind.Utc));

                writer.WritePropertyName("Prop2");
                writer.WriteValue("This is an escaped \n string!");

                writer.WriteValue("Error!");
            }

            XUnitAssert.Fail();
        }
        catch
        {
        }

        Assert.Equal(0, arrayPool.UsedArrays.Count);
        Assert.Equal(1, arrayPool.FreeArrays.Count);
    }

#if !RELEASE
    [Fact]
    public void BufferErroringWithInvalidSize()
    {
        var o = new JObject
        {
            {"BodyHtml", $"<h3>Title!</h3>{Environment.NewLine}{new string(' ', 100)}<p>Content!</p>"}
        };

        var arrayPool = new JsonArrayPool();

        var stringWriter = new StringWriter();
        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            jsonWriter.ArrayPool = arrayPool;

            o.WriteTo(jsonWriter);
        }

        var result = o.ToString();

        XUnitAssert.AreEqualNormalized(@"{
  ""BodyHtml"": ""<h3>Title!</h3>\r\n                                                                                                    <p>Content!</p>""
}", result);
    }
#endif

    [Fact]
    public void NewLine()
    {
        var ms = new MemoryStream();

        using (var streamWriter = new StreamWriter(ms, new UTF8Encoding(false)) { NewLine = "\n" })
        using (var jsonWriter = new JsonTextWriter(streamWriter)
               {
                   CloseOutput = true,
                   Indentation = 2,
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("prop");
            jsonWriter.WriteValue(true);
            jsonWriter.WriteEndObject();
        }

        var data = ms.ToArray();

        var json = Encoding.UTF8.GetString(data, 0, data.Length);

        XUnitAssert.EqualsNormalized(@"{
  ""prop"": true
}", json);
    }

    [Fact]
    public void QuoteNameAndStrings()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);
        var jsonWriter = new JsonTextWriter(stringWriter) { QuoteName = false };

        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("name");
        jsonWriter.WriteValue("value");

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        Assert.Equal(@"{name:""value""}", stringBuilder.ToString());
    }

    [Fact]
    public void CloseOutput()
    {
        var ms = new MemoryStream();
        var writer = new JsonTextWriter(new StreamWriter(ms));

        Assert.True(ms.CanRead);
        writer.Close();
        Assert.False(ms.CanRead);

        ms = new MemoryStream();
        writer = new JsonTextWriter(new StreamWriter(ms)) { CloseOutput = false };

        Assert.True(ms.CanRead);
        writer.Close();
        Assert.True(ms.CanRead);
    }

    [Fact]
    public void WriteIConvertable()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);
        jsonWriter.WriteValue(new ConvertibleInt(1));

        Assert.Equal("1", stringWriter.ToString());
    }

    [Fact]
    public void ValueFormatting()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue('@');
            jsonWriter.WriteValue("\r\n\t\f\b?{\\r\\n\"\'");
            jsonWriter.WriteValue(true);
            jsonWriter.WriteValue(10);
            jsonWriter.WriteValue(10.99);
            jsonWriter.WriteValue(0.99);
            jsonWriter.WriteValue(0.000000000000000001d);
            jsonWriter.WriteValue(0.000000000000000001m);
            jsonWriter.WriteValue((string)null);
            jsonWriter.WriteValue((object)null);
            jsonWriter.WriteValue("This is a string.");
            jsonWriter.WriteNull();
            jsonWriter.WriteUndefined();
            jsonWriter.WriteEndArray();
        }

        var expected = @"[""@"",""\r\n\t\f\b?{\\r\\n\""'"",true,10,10.99,0.99,1E-18,0.000000000000000001,null,null,""This is a string."",null,undefined]";
        var result = stringBuilder.ToString();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void NullableValueFormatting()
    {
        var stringWriter = new StringWriter();
        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue((char?)null);
            jsonWriter.WriteValue((char?)'c');
            jsonWriter.WriteValue((bool?)null);
            jsonWriter.WriteValue((bool?)true);
            jsonWriter.WriteValue((byte?)null);
            jsonWriter.WriteValue((byte?)1);
            jsonWriter.WriteValue((sbyte?)null);
            jsonWriter.WriteValue((sbyte?)1);
            jsonWriter.WriteValue((short?)null);
            jsonWriter.WriteValue((short?)1);
            jsonWriter.WriteValue((ushort?)null);
            jsonWriter.WriteValue((ushort?)1);
            jsonWriter.WriteValue((int?)null);
            jsonWriter.WriteValue((int?)1);
            jsonWriter.WriteValue((uint?)null);
            jsonWriter.WriteValue((uint?)1);
            jsonWriter.WriteValue((long?)null);
            jsonWriter.WriteValue((long?)1);
            jsonWriter.WriteValue((ulong?)null);
            jsonWriter.WriteValue((ulong?)1);
            jsonWriter.WriteValue((double?)null);
            jsonWriter.WriteValue((double?)1.1);
            jsonWriter.WriteValue((float?)null);
            jsonWriter.WriteValue((float?)1.1);
            jsonWriter.WriteValue((decimal?)null);
            jsonWriter.WriteValue((decimal?)1.1m);
            jsonWriter.WriteValue((DateTime?)null);
            jsonWriter.WriteValue((DateTime?)new DateTime(TestFixtureBase.InitialJavaScriptDateTicks, DateTimeKind.Utc));
            jsonWriter.WriteValue((DateTimeOffset?)null);
            jsonWriter.WriteValue((DateTimeOffset?)new DateTimeOffset(TestFixtureBase.InitialJavaScriptDateTicks, TimeSpan.Zero));
            jsonWriter.WriteEndArray();
        }

        var json = stringWriter.ToString();

        var expected = @"[null,""c"",null,true,null,1,null,1,null,1,null,1,null,1,null,1,null,1,null,1,null,1.1,null,1.1,null,1.1,null,""1970-01-01T00:00:00Z"",null,""1970-01-01T00:00:00+00:00""]";

        Assert.Equal(expected, json);
    }

    [Fact]
    public void WriteValueObjectWithNullable()
    {
        var stringWriter = new StringWriter();
        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            char? value = 'c';

            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue((object)value);
            jsonWriter.WriteEndArray();
        }

        var json = stringWriter.ToString();
        var expected = @"[""c""]";

        Assert.Equal(expected, json);
    }

    [Fact]
    public void WriteValueObjectWithUnsupportedValue()
    {
        XUnitAssert.Throws<JsonWriterException>(
            () =>
            {
                var stringWriter = new StringWriter();
                using var jsonWriter = new JsonTextWriter(stringWriter);
                jsonWriter.WriteStartArray();
                jsonWriter.WriteValue(new Version(1, 1, 1, 1));
                jsonWriter.WriteEndArray();
            },
            @"Unsupported type: System.Version. Use the JsonSerializer class to get the object's JSON representation. Path ''.");
    }

    [Fact]
    public void StringEscaping()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue(@"""These pretzels are making me thirsty!""");
            jsonWriter.WriteValue("Jeff's house was burninated.");
            jsonWriter.WriteValue("1. You don't talk about fight club.\r\n2. You don't talk about fight club.");
            jsonWriter.WriteValue("35% of\t statistics\n are made\r up.");
            jsonWriter.WriteEndArray();
        }

        var expected = @"[""\""These pretzels are making me thirsty!\"""",""Jeff's house was burninated."",""1. You don't talk about fight club.\r\n2. You don't talk about fight club."",""35% of\t statistics\n are made\r up.""]";
        var result = stringBuilder.ToString();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteEnd()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("CPU");
            jsonWriter.WriteValue("Intel");
            jsonWriter.WritePropertyName("PSU");
            jsonWriter.WriteValue("500W");
            jsonWriter.WritePropertyName("Drives");
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue("DVD read/writer");
            jsonWriter.WriteComment("(broken)");
            jsonWriter.WriteValue("500 gigabyte hard drive");
            jsonWriter.WriteValue("200 gigabyte hard drive");
            jsonWriter.WriteEndObject();
            Assert.Equal(WriteState.Start, jsonWriter.WriteState);
        }

        var expected = @"{
  ""CPU"": ""Intel"",
  ""PSU"": ""500W"",
  ""Drives"": [
    ""DVD read/writer""
    /*(broken)*/,
    ""500 gigabyte hard drive"",
    ""200 gigabyte hard drive""
  ]
}";
        var result = stringBuilder.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void CloseWithRemainingContent()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("CPU");
            jsonWriter.WriteValue("Intel");
            jsonWriter.WritePropertyName("PSU");
            jsonWriter.WriteValue("500W");
            jsonWriter.WritePropertyName("Drives");
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue("DVD read/writer");
            jsonWriter.WriteComment("(broken)");
            jsonWriter.WriteValue("500 gigabyte hard drive");
            jsonWriter.WriteValue("200 gigabyte hard drive");
            jsonWriter.Close();
        }

        var expected = @"{
  ""CPU"": ""Intel"",
  ""PSU"": ""500W"",
  ""Drives"": [
    ""DVD read/writer""
    /*(broken)*/,
    ""500 gigabyte hard drive"",
    ""200 gigabyte hard drive""
  ]
}";
        var result = stringBuilder.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void Indenting()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("CPU");
            jsonWriter.WriteValue("Intel");
            jsonWriter.WritePropertyName("PSU");
            jsonWriter.WriteValue("500W");
            jsonWriter.WritePropertyName("Drives");
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue("DVD read/writer");
            jsonWriter.WriteComment("(broken)");
            jsonWriter.WriteValue("500 gigabyte hard drive");
            jsonWriter.WriteValue("200 gigabyte hard drive");
            jsonWriter.WriteEnd();
            jsonWriter.WriteEndObject();
            Assert.Equal(WriteState.Start, jsonWriter.WriteState);
        }

        // {
        //   "CPU": "Intel",
        //   "PSU": "500W",
        //   "Drives": [
        //     "DVD read/writer"
        //     /*(broken)*/,
        //     "500 gigabyte hard drive",
        //     "200 gigabyte hard drive"
        //   ]
        // }

        var expected = @"{
  ""CPU"": ""Intel"",
  ""PSU"": ""500W"",
  ""Drives"": [
    ""DVD read/writer""
    /*(broken)*/,
    ""500 gigabyte hard drive"",
    ""200 gigabyte hard drive""
  ]
}";
        var result = stringBuilder.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void State()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using var jsonWriter = new JsonTextWriter(stringWriter);
        Assert.Equal(WriteState.Start, jsonWriter.WriteState);

        jsonWriter.WriteStartObject();
        Assert.Equal(WriteState.Object, jsonWriter.WriteState);
        Assert.Equal("", jsonWriter.Path);

        jsonWriter.WritePropertyName("CPU");
        Assert.Equal(WriteState.Property, jsonWriter.WriteState);
        Assert.Equal("CPU", jsonWriter.Path);

        jsonWriter.WriteValue("Intel");
        Assert.Equal(WriteState.Object, jsonWriter.WriteState);
        Assert.Equal("CPU", jsonWriter.Path);

        jsonWriter.WritePropertyName("Drives");
        Assert.Equal(WriteState.Property, jsonWriter.WriteState);
        Assert.Equal("Drives", jsonWriter.Path);

        jsonWriter.WriteStartArray();
        Assert.Equal(WriteState.Array, jsonWriter.WriteState);

        jsonWriter.WriteValue("DVD read/writer");
        Assert.Equal(WriteState.Array, jsonWriter.WriteState);
        Assert.Equal("Drives[0]", jsonWriter.Path);

        jsonWriter.WriteEnd();
        Assert.Equal(WriteState.Object, jsonWriter.WriteState);
        Assert.Equal("Drives", jsonWriter.Path);

        jsonWriter.WriteEndObject();
        Assert.Equal(WriteState.Start, jsonWriter.WriteState);
        Assert.Equal("", jsonWriter.Path);
    }

    [Fact]
    public void FloatingPointNonFiniteNumbers_Symbol()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.FloatFormatHandling = FloatFormatHandling.Symbol;

            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue(double.NaN);
            jsonWriter.WriteValue(double.PositiveInfinity);
            jsonWriter.WriteValue(double.NegativeInfinity);
            jsonWriter.WriteValue(float.NaN);
            jsonWriter.WriteValue(float.PositiveInfinity);
            jsonWriter.WriteValue(float.NegativeInfinity);
            jsonWriter.WriteEndArray();

            jsonWriter.Flush();
        }

        var expected = @"[
  NaN,
  Infinity,
  -Infinity,
  NaN,
  Infinity,
  -Infinity
]";
        var result = stringBuilder.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void FloatingPointNonFiniteNumbers_Zero()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented,
            FloatFormatHandling = FloatFormatHandling.DefaultValue
               })
        {
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue(double.NaN);
            jsonWriter.WriteValue(double.PositiveInfinity);
            jsonWriter.WriteValue(double.NegativeInfinity);
            jsonWriter.WriteValue(float.NaN);
            jsonWriter.WriteValue(float.PositiveInfinity);
            jsonWriter.WriteValue(float.NegativeInfinity);
            jsonWriter.WriteValue((double?)double.NaN);
            jsonWriter.WriteValue((double?)double.PositiveInfinity);
            jsonWriter.WriteValue((double?)double.NegativeInfinity);
            jsonWriter.WriteValue((float?)float.NaN);
            jsonWriter.WriteValue((float?)float.PositiveInfinity);
            jsonWriter.WriteValue((float?)float.NegativeInfinity);
            jsonWriter.WriteEndArray();

            jsonWriter.Flush();
        }

        var expected = @"[
  0.0,
  0.0,
  0.0,
  0.0,
  0.0,
  0.0,
  null,
  null,
  null,
  null,
  null,
  null
]";
        var result = stringBuilder.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void FloatingPointNonFiniteNumbers_String()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented,
            FloatFormatHandling = FloatFormatHandling.String
               })
        {
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue(double.NaN);
            jsonWriter.WriteValue(double.PositiveInfinity);
            jsonWriter.WriteValue(double.NegativeInfinity);
            jsonWriter.WriteValue(float.NaN);
            jsonWriter.WriteValue(float.PositiveInfinity);
            jsonWriter.WriteValue(float.NegativeInfinity);
            jsonWriter.WriteEndArray();

            jsonWriter.Flush();
        }

        var expected = @"[
  ""NaN"",
  ""Infinity"",
  ""-Infinity"",
  ""NaN"",
  ""Infinity"",
  ""-Infinity""
]";
        var result = stringBuilder.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void FloatingPointNonFiniteNumbers_QuoteChar()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented,
            FloatFormatHandling = FloatFormatHandling.String,
            QuoteChar = '\''
               })
        {
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue(double.NaN);
            jsonWriter.WriteValue(double.PositiveInfinity);
            jsonWriter.WriteValue(double.NegativeInfinity);
            jsonWriter.WriteValue(float.NaN);
            jsonWriter.WriteValue(float.PositiveInfinity);
            jsonWriter.WriteValue(float.NegativeInfinity);
            jsonWriter.WriteEndArray();

            jsonWriter.Flush();
        }

        var expected = @"[
  'NaN',
  'Infinity',
  '-Infinity',
  'NaN',
  'Infinity',
  '-Infinity'
]";
        var result = stringBuilder.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void WriteRawInStart()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented,
            FloatFormatHandling = FloatFormatHandling.Symbol
               })
        {
            jsonWriter.WriteRaw("[1,2,3,4,5]");
            jsonWriter.WriteWhitespace("  ");
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue(double.NaN);
            jsonWriter.WriteEndArray();
        }

        var expected = @"[1,2,3,4,5]  [
  NaN
]";
        var result = stringBuilder.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void WriteRawInArray()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.FloatFormatHandling = FloatFormatHandling.Symbol;

            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue(double.NaN);
            jsonWriter.WriteRaw(",[1,2,3,4,5]");
            jsonWriter.WriteRaw(",[1,2,3,4,5]");
            jsonWriter.WriteValue(float.NaN);
            jsonWriter.WriteEndArray();
        }

        var expected = @"[
  NaN,[1,2,3,4,5],[1,2,3,4,5],
  NaN
]";
        var result = stringBuilder.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void WriteRawInObject()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteRaw(@"""PropertyName"":[1,2,3,4,5]");
            jsonWriter.WriteEnd();
        }

        var expected = @"{""PropertyName"":[1,2,3,4,5]}";
        var result = stringBuilder.ToString();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteToken()
    {
        var reader = new JsonTextReader(new StringReader("[1,2,3,4,5]"));
        reader.Read();
        reader.Read();

        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);
        jsonWriter.WriteToken(reader);

        Assert.Equal("1", stringWriter.ToString());
    }

    [Fact]
    public void WriteRawValue()
    {
        var stringBuilder = new StringBuilder();
        var textWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(textWriter))
        {
            var i = 0;
            var rawJson = "[1,2]";

            jsonWriter.WriteStartObject();

            while (i < 3)
            {
                jsonWriter.WritePropertyName($"d{i}");
                jsonWriter.WriteRawValue(rawJson);

                i++;
            }

            jsonWriter.WriteEndObject();
        }

        Assert.Equal(@"{""d0"":[1,2],""d1"":[1,2],""d2"":[1,2]}", stringBuilder.ToString());
    }

    [Fact]
    public void WriteFloatingPointNumber()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            jsonWriter.FloatFormatHandling = FloatFormatHandling.Symbol;

            jsonWriter.WriteStartArray();

            jsonWriter.WriteValue(0.0);
            jsonWriter.WriteValue(0f);
            jsonWriter.WriteValue(0.1);
            jsonWriter.WriteValue(1.0);
            jsonWriter.WriteValue(1.000001);
            jsonWriter.WriteValue(0.000001);
            jsonWriter.WriteValue(double.Epsilon);
            jsonWriter.WriteValue(double.PositiveInfinity);
            jsonWriter.WriteValue(double.NegativeInfinity);
            jsonWriter.WriteValue(double.NaN);
            jsonWriter.WriteValue(double.MaxValue);
            jsonWriter.WriteValue(double.MinValue);
            jsonWriter.WriteValue(float.PositiveInfinity);
            jsonWriter.WriteValue(float.NegativeInfinity);
            jsonWriter.WriteValue(float.NaN);

            jsonWriter.WriteEndArray();
        }

#if (NET5_0_OR_GREATER)
        Assert.Equal(@"[0.0,0.0,0.1,1.0,1.000001,1E-06,5E-324,Infinity,-Infinity,NaN,1.7976931348623157E+308,-1.7976931348623157E+308,Infinity,-Infinity,NaN]", stringBuilder.ToString());
#else
        Assert.Equal(@"[0.0,0.0,0.1,1.0,1.000001,1E-06,4.94065645841247E-324,Infinity,-Infinity,NaN,1.7976931348623157E+308,-1.7976931348623157E+308,Infinity,-Infinity,NaN]", stringBuilder.ToString());
#endif

    }

    [Fact]
    public void WriteIntegerNumber()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented })
        {
            jsonWriter.WriteStartArray();

            jsonWriter.WriteValue(int.MaxValue);
            jsonWriter.WriteValue(int.MinValue);
            jsonWriter.WriteValue(0);
            jsonWriter.WriteValue(-0);
            jsonWriter.WriteValue(9L);
            jsonWriter.WriteValue(9UL);
            jsonWriter.WriteValue(long.MaxValue);
            jsonWriter.WriteValue(long.MinValue);
            jsonWriter.WriteValue(ulong.MaxValue);
            jsonWriter.WriteValue(ulong.MinValue);
            jsonWriter.WriteValue((ulong)uint.MaxValue - 1);
            jsonWriter.WriteValue((ulong)uint.MaxValue);
            jsonWriter.WriteValue((ulong)uint.MaxValue + 1);

            jsonWriter.WriteEndArray();
        }

        Console.WriteLine(stringBuilder.ToString());

        XUnitAssert.AreEqualNormalized(@"[
  2147483647,
  -2147483648,
  0,
  0,
  9,
  9,
  9223372036854775807,
  -9223372036854775808,
  18446744073709551615,
  0,
  4294967294,
  4294967295,
  4294967296
]", stringBuilder.ToString());
    }

    [Fact]
    public void WriteTokenDirect()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            jsonWriter.WriteToken(JsonToken.StartArray);
            jsonWriter.WriteToken(JsonToken.Integer, 1);
            jsonWriter.WriteToken(JsonToken.StartObject);
            jsonWriter.WriteToken(JsonToken.PropertyName, "integer");
            jsonWriter.WriteToken(JsonToken.Integer, int.MaxValue);
            jsonWriter.WriteToken(JsonToken.PropertyName, "null-string");
            jsonWriter.WriteToken(JsonToken.String, null);
            jsonWriter.WriteToken(JsonToken.EndObject);
            jsonWriter.WriteToken(JsonToken.EndArray);
        }

        Assert.Equal(@"[1,{""integer"":2147483647,""null-string"":null}]", stringBuilder.ToString());
    }

    [Fact]
    public void WriteTokenDirect_BadValue()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using var jsonWriter = new JsonTextWriter(stringWriter);
        jsonWriter.WriteToken(JsonToken.StartArray);

        XUnitAssert.Throws<FormatException>(
            () => jsonWriter.WriteToken(JsonToken.Integer, "three"),
            "Input string was not in a correct format.");
    }

    [Fact]
    public void TokenTypeOutOfRange()
    {
        using var jsonWriter = new JsonTextWriter(new StringWriter());
        var ex = XUnitAssert.Throws<ArgumentOutOfRangeException>(() => jsonWriter.WriteToken((JsonToken)int.MinValue));
        Assert.Equal("token", ex.ParamName);
    }

    [Fact]
    public void BadWriteEndArray()
    {
        XUnitAssert.Throws<JsonWriterException>(
            () =>
            {
                var stringBuilder = new StringBuilder();
                var stringWriter = new StringWriter(stringBuilder);

                using var jsonWriter = new JsonTextWriter(stringWriter);
                jsonWriter.WriteStartArray();

                jsonWriter.WriteValue(0.0);

                jsonWriter.WriteEndArray();
                jsonWriter.WriteEndArray();
            },
            "No token to close. Path ''.");
    }

    [Fact]
    public void InvalidQuoteChar()
    {
        XUnitAssert.Throws<ArgumentException>(
            () =>
            {
                var stringBuilder = new StringBuilder();
                var stringWriter = new StringWriter(stringBuilder);

                using var jsonWriter = new JsonTextWriter(stringWriter);
                jsonWriter.QuoteChar = '*';
            },
            @"Invalid JavaScript string quote character. Valid quote characters are ' and "".");
    }

    [Fact]
    public void Indentation()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented,
            FloatFormatHandling = FloatFormatHandling.Symbol
               })
        {
            Assert.Equal(Formatting.Indented, jsonWriter.Formatting);

            jsonWriter.Indentation = 5;
            Assert.Equal(5, jsonWriter.Indentation);
            jsonWriter.IndentChar = '_';
            Assert.Equal('_', jsonWriter.IndentChar);
            jsonWriter.QuoteName = true;
            XUnitAssert.True(jsonWriter.QuoteName);
            jsonWriter.QuoteChar = '\'';
            Assert.Equal('\'', jsonWriter.QuoteChar);

            jsonWriter.WriteStartObject();

            jsonWriter.WritePropertyName("propertyName");
            jsonWriter.WriteValue(double.NaN);

            jsonWriter.IndentChar = '?';
            Assert.Equal('?', jsonWriter.IndentChar);
            jsonWriter.Indentation = 6;
            Assert.Equal(6, jsonWriter.Indentation);

            jsonWriter.WritePropertyName("prop2");
            jsonWriter.WriteValue(123);

            jsonWriter.WriteEndObject();
        }

        var expected = @"{
_____'propertyName': NaN,
??????'prop2': 123
}";
        var result = stringBuilder.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void WriteSingleBytes()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        var text = "Hello world.";
        var data = Encoding.UTF8.GetBytes(text);

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteValue(data);
        }

        var expected = @"""SGVsbG8gd29ybGQu""";
        var result = stringBuilder.ToString();

        Assert.Equal(expected, result);

        var d2 = Convert.FromBase64String(result.Trim('"'));

        Assert.Equal(text, Encoding.UTF8.GetString(d2, 0, d2.Length));
    }

    [Fact]
    public void WriteBytesInArray()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        var text = "Hello world.";
        var data = Encoding.UTF8.GetBytes(text);

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue(data);
            jsonWriter.WriteValue(data);
            jsonWriter.WriteValue((object)data);
            jsonWriter.WriteValue((byte[])null);
            jsonWriter.WriteValue((Uri)null);
            jsonWriter.WriteEndArray();
        }

        var expected = @"[
  ""SGVsbG8gd29ybGQu"",
  ""SGVsbG8gd29ybGQu"",
  ""SGVsbG8gd29ybGQu"",
  null,
  null
]";
        var result = stringBuilder.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public Task Path()
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteStartArray();
            Assert.Equal("", jsonWriter.Path);
            jsonWriter.WriteStartObject();
            Assert.Equal("[0]", jsonWriter.Path);
            jsonWriter.WritePropertyName("Property1");
            Assert.Equal("[0].Property1", jsonWriter.Path);
            jsonWriter.WriteStartArray();
            Assert.Equal("[0].Property1", jsonWriter.Path);
            jsonWriter.WriteValue(1);
            Assert.Equal("[0].Property1[0]", jsonWriter.Path);
            jsonWriter.WriteStartArray();
            Assert.Equal("[0].Property1[1]", jsonWriter.Path);
            jsonWriter.WriteStartArray();
            Assert.Equal("[0].Property1[1][0]", jsonWriter.Path);
            jsonWriter.WriteStartArray();
            Assert.Equal("[0].Property1[1][0][0]", jsonWriter.Path);
            jsonWriter.WriteEndObject();
            Assert.Equal("[0]", jsonWriter.Path);
            jsonWriter.WriteStartObject();
            Assert.Equal("[1]", jsonWriter.Path);
            jsonWriter.WritePropertyName("Property2");
            Assert.Equal("[1].Property2", jsonWriter.Path);
            jsonWriter.WriteNull();
            Assert.Equal("[1].Property2", jsonWriter.Path);
        }

        return Verify(stringBuilder);
    }

    [Fact]
    public void BuildStateArray()
    {
        var stateArray = JsonWriter.BuildStateArray();

        var valueStates = JsonWriter.StateArrayTemplate[6];

        foreach (JsonToken valueToken in GetValues(typeof(JsonToken)))
        {
            switch (valueToken)
            {
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.String:
                case JsonToken.Boolean:
                case JsonToken.Null:
                case JsonToken.Undefined:
                case JsonToken.Date:
                case JsonToken.Bytes:
                    Assert.Equal(valueStates, stateArray[(int)valueToken]);
                    break;
            }
        }
    }

    static IList<object> GetValues(Type enumType)
    {
        if (!enumType.IsEnum)
        {
            throw new ArgumentException($"Type {enumType.Name} is not an enum.", nameof(enumType));
        }

        var values = new List<object>();

        foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var value = field.GetValue(enumType);
            values.Add(value);
        }

        return values;
    }

    [Fact]
    public void DateTimeZoneHandling()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            DateTimeZoneHandling = Argon.DateTimeZoneHandling.Utc
        };

        jsonWriter.WriteValue(new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Unspecified));

        Assert.Equal(@"""2000-01-01T01:01:01Z""", stringWriter.ToString());
    }

    [Fact]
    public void HtmlEscapeHandling()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            EscapeHandling = EscapeHandling.EscapeHtml
        };

        var script = @"<script type=""text/javascript"">alert('hi');</script>";

        jsonWriter.WriteValue(script);

        var json = stringWriter.ToString();

        Assert.Equal(@"""\u003cscript type=\u0022text/javascript\u0022\u003ealert(\u0027hi\u0027);\u003c/script\u003e""", json);

        var reader = new JsonTextReader(new StringReader(json));

        Assert.Equal(script, reader.ReadAsString());
    }

    [Fact]
    public void NonAsciiEscapeHandling()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            EscapeHandling = EscapeHandling.EscapeNonAscii
        };

        var unicode = "\u5f20";

        jsonWriter.WriteValue(unicode);

        var json = stringWriter.ToString();

        Assert.Equal(8, json.Length);
        Assert.Equal(@"""\u5f20""", json);

        var reader = new JsonTextReader(new StringReader(json));

        Assert.Equal(unicode, reader.ReadAsString());

        stringWriter = new StringWriter();
        jsonWriter = new JsonTextWriter(stringWriter)
        {
            EscapeHandling = EscapeHandling.Default
        };

        jsonWriter.WriteValue(unicode);

        json = stringWriter.ToString();

        Assert.Equal(3, json.Length);
        Assert.Equal("\"\u5f20\"", json);
    }

    [Fact]
    public void WriteEndOnProperty()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);
        jsonWriter.QuoteChar = '\'';

        jsonWriter.WriteStartObject();
        jsonWriter.WritePropertyName("Blah");
        jsonWriter.WriteEnd();

        Assert.Equal("{'Blah':null}", stringWriter.ToString());
    }

    [Fact]
    public void WriteEndOnProperty_Close()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);
        jsonWriter.QuoteChar = '\'';

        jsonWriter.WriteStartObject();
        jsonWriter.WritePropertyName("Blah");
        jsonWriter.Close();

        Assert.Equal("{'Blah':null}", stringWriter.ToString());
    }

    [Fact]
    public void WriteEndOnProperty_Dispose()
    {
        var stringWriter = new StringWriter();
        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            jsonWriter.QuoteChar = '\'';

            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("Blah");
        }

        Assert.Equal("{'Blah':null}", stringWriter.ToString());
    }

    [Fact]
    public void AutoCompleteOnClose_False()
    {
        var stringWriter = new StringWriter();
        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
            AutoCompleteOnClose = false,
            QuoteChar = '\''
               })
        {

            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("Blah");
        }

        Assert.Equal("{'Blah':", stringWriter.ToString());
    }

    [Fact]
    public void QuoteChar()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            Formatting = Formatting.Indented,
            QuoteChar = '\''
        };

        jsonWriter.WriteStartArray();

        jsonWriter.WriteValue(new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc));
        jsonWriter.WriteValue(new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero));

        jsonWriter.DateFormatString = "yyyy gg";
        jsonWriter.WriteValue(new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc));
        jsonWriter.WriteValue(new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero));

        jsonWriter.WriteValue(new byte[] { 1, 2, 3 });
        jsonWriter.WriteValue(TimeSpan.Zero);
        jsonWriter.WriteValue(new Uri("http://www.google.com/"));
        jsonWriter.WriteValue(Guid.Empty);

        jsonWriter.WriteEnd();

        XUnitAssert.AreEqualNormalized(@"[
  '2000-01-01T01:01:01Z',
  '2000-01-01T01:01:01+00:00',
  '2000 A.D.',
  '2000 A.D.',
  'AQID',
  '00:00:00',
  'http://www.google.com/',
  '00000000-0000-0000-0000-000000000000'
]", stringWriter.ToString());
    }

    [Fact]
    public void Culture()
    {
        var culture = new CultureInfo("en-NZ")
        {
            DateTimeFormat =
            {
                AMDesignator = "a.m.",
                PMDesignator = "p.m."
            }
        };

        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            Formatting = Formatting.Indented,
            DateFormatString = "yyyy tt",
            Culture = culture,
            QuoteChar = '\''
        };

        jsonWriter.WriteStartArray();

        jsonWriter.WriteValue(new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc));
        jsonWriter.WriteValue(new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero));

        jsonWriter.WriteEnd();

        XUnitAssert.AreEqualNormalized(@"[
  '2000 a.m.',
  '2000 a.m.'
]", stringWriter.ToString());
    }

    [Fact]
    public void CustomJsonTextWriterTests()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new CustomJsonTextWriter(stringWriter)
        {
            Formatting = Formatting.Indented
        };
        jsonWriter.WriteStartObject();
        Assert.Equal(WriteState.Object, jsonWriter.WriteState);
        jsonWriter.WritePropertyName("Property1");
        Assert.Equal(WriteState.Property, jsonWriter.WriteState);
        Assert.Equal("Property1", jsonWriter.Path);
        jsonWriter.WriteNull();
        Assert.Equal(WriteState.Object, jsonWriter.WriteState);
        jsonWriter.WriteEndObject();
        Assert.Equal(WriteState.Start, jsonWriter.WriteState);

        XUnitAssert.AreEqualNormalized(@"{{{
  ""1ytreporP"": NULL!!!
}}}", stringWriter.ToString());
    }

    [Fact]
    public void QuoteDictionaryNames()
    {
        var d = new Dictionary<string, int>
        {
            { "a", 1 },
        };
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
        };
        var serializer = JsonSerializer.Create(settings);
        using var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter)
               {
                   QuoteName = false
               })
        {
            serializer.Serialize(writer, d);
            writer.Close();
        }

        XUnitAssert.AreEqualNormalized(@"{
  a: 1
}", stringWriter.ToString());
    }

    [Fact]
    public void WriteComments()
    {
        var json = $@"//comment*//*hi*/
{{//comment
Name://comment
true//comment after true{StringUtils.CarriageReturn}
,//comment after comma{StringUtils.CarriageReturnLineFeed}
ExpiryDate: '2014-06-04T00:00:00Z',
        Price: 3.99,
        Sizes: //comment
[//comment

          ""Small""//comment
]//comment
}}//comment 
//comment 1 ";

        var r = new JsonTextReader(new StringReader(json));

        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);
        jsonWriter.Formatting = Formatting.Indented;

        jsonWriter.WriteToken(r, true);

        XUnitAssert.AreEqualNormalized(@"/*comment*//*hi*/*/{/*comment*/
  ""Name"": /*comment*/ true/*comment after true*//*comment after comma*/,
  ""ExpiryDate"": ""2014-06-04T00:00:00Z"",
  ""Price"": 3.99,
  ""Sizes"": /*comment*/ [
    /*comment*/
    ""Small""
    /*comment*/
  ]/*comment*/
}/*comment *//*comment 1 */", stringWriter.ToString());
    }

    [Fact]
    public void DisposeSupressesFinalization()
    {
        UnmanagedResourceFakingJsonWriter.CreateAndDispose();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Assert.Equal(1, UnmanagedResourceFakingJsonWriter.DisposalCalls);
    }
}

public class CustomJsonTextWriter : JsonTextWriter
{
    protected readonly TextWriter writer;

    public CustomJsonTextWriter(TextWriter textWriter) : base(textWriter)
    {
        writer = textWriter;
    }

    public override void WritePropertyName(string name)
    {
        WritePropertyName(name, true);
    }

    public override void WritePropertyName(string name, bool escape)
    {
        SetWriteState(JsonToken.PropertyName, name);

        if (QuoteName)
        {
            writer.Write(QuoteChar);
        }

        writer.Write(new string(name.ToCharArray().Reverse().ToArray()));

        if (QuoteName)
        {
            writer.Write(QuoteChar);
        }

        writer.Write(':');
    }

    public override void WriteNull()
    {
        SetWriteState(JsonToken.Null, null);

        writer.Write("NULL!!!");
    }

    public override void WriteStartObject()
    {
        SetWriteState(JsonToken.StartObject, null);

        writer.Write("{{{");
    }

    public override void WriteEndObject()
    {
        SetWriteState(JsonToken.EndObject, null);
    }

    protected override void WriteEnd(JsonToken token)
    {
        if (token == JsonToken.EndObject)
        {
            writer.Write("}}}");
        }
        else
        {
            base.WriteEnd(token);
        }
    }
}

public class UnmanagedResourceFakingJsonWriter : JsonWriter
{
    public static int DisposalCalls;

    public static void CreateAndDispose()
    {
        ((IDisposable) new UnmanagedResourceFakingJsonWriter()).Dispose();
    }

    public UnmanagedResourceFakingJsonWriter()
    {
        DisposalCalls = 0;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        ++DisposalCalls;
    }

    ~UnmanagedResourceFakingJsonWriter()
    {
        Dispose(false);
    }

    public override void Flush()
    {
        throw new NotImplementedException();
    }
}