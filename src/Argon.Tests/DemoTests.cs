﻿// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.Buffers;

public class DemoTests : TestFixtureBase
{
    public class HtmlColor
    {
        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }
    }

    [Fact]
    public void JsonConverter()
    {
        var red = new HtmlColor
        {
            Red = 255,
            Green = 0,
            Blue = 0
        };

        var json = JsonConvert.SerializeObject(red, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        });
        // {
        //   "Red": 255,
        //   "Green": 0,
        //   "Blue": 0
        // }

        json = JsonConvert.SerializeObject(red, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = {new HtmlColorConverter()}
        });
        // "#FF0000"

        var r2 = JsonConvert.DeserializeObject<HtmlColor>(json, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = {new HtmlColorConverter()}
        });
        Assert.Equal(255, r2.Red);
        Assert.Equal(0, r2.Green);
        Assert.Equal(0, r2.Blue);

        Assert.Equal(@"""#FF0000""", json);
    }

    public class PersonDemo
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Job { get; set; }
    }

    public class Session
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
    }

    public class HtmlColorConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // create hex string from value
            var color = (HtmlColor) value;
            var hexString = color.Red.ToString("X2")
                            + color.Green.ToString("X2")
                            + color.Blue.ToString("X2");

            // write value to json
            writer.WriteValue($"#{hexString}");
        }

        //public override object ReadJson(JsonReader reader, Type type,
        //    object existingValue, JsonSerializer serializer)
        //{
        //    throw new NotImplementedException();
        //}

        public override object ReadJson(JsonReader reader, Type type,
            object existingValue, JsonSerializer serializer)
        {
            // get hex string
            var hexString = (string) reader.Value;
            hexString = hexString.TrimStart('#');

            // build html color from hex
            return new HtmlColor
            {
                Red = Convert.ToInt32(hexString.Substring(0, 2), 16),
                Green = Convert.ToInt32(hexString.Substring(2, 2), 16),
                Blue = Convert.ToInt32(hexString.Substring(4, 2), 16)
            };
        }

        public override bool CanConvert(Type type)
        {
            return type == typeof(HtmlColor);
        }
    }

    [Fact]
    public void SerializationGuide()
    {
        var roles = new List<string>
        {
            "User",
            "Admin"
        };

        var roleJson = JsonConvert.SerializeObject(roles, Formatting.Indented);
        // [
        //   "User",
        //   "Admin"
        // ]

        var dailyRegistrations = new Dictionary<DateTime, int>
        {
            {new DateTime(2014, 6, 1), 23},
            {new DateTime(2014, 6, 2), 50}
        };

        var regJson = JsonConvert.SerializeObject(dailyRegistrations, Formatting.Indented);
        // {
        //   "2014-06-01T00:00:00": 23,
        //   "2014-06-02T00:00:00": 50
        // }

        var c = new City {Name = "Oslo", Population = 650000};

        var cityJson = JsonConvert.SerializeObject(c, Formatting.Indented);
        // {
        //   "Name": "Oslo",
        //   "Population": 650000
        // }
    }

    [Fact]
    public void SerializationBasics()
    {
        var roles = new List<string>
        {
            "User",
            "Admin"
        };

        var traceWriter = new MemoryTraceWriter();

        var j = JsonConvert.SerializeObject(roles, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TraceWriter = traceWriter
        });

        var trace = traceWriter.ToString();
        // Started serializing System.Collections.Generic.List`1[System.String].
        // Finished serializing System.Collections.Generic.List`1[System.String].
        // Verbose Serialized JSON:
        // [
        //   "User",
        //   "Admin"
        // ]
    }

    [Fact]
    public void SerializationBasics2()
    {
        var s = new Session
        {
            Name = "Serialize All The Things",
            Date = new DateTime(2014, 6, 4, 0, 0, 0, DateTimeKind.Utc)
        };

        var j = JsonConvert.SerializeObject(s, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
        });
        // {
        //   "Name": "Serialize All The Things",
        //   "Date": new Date(1401796800000)
        // }

        XUnitAssert.AreEqualNormalized(@"{
  ""Name"": ""Serialize All The Things"",
  ""Date"": ""2014-06-04T00:00:00Z""
}", j);
    }

    [Fact]
    public void DeserializationBasics1()
    {
        var j = @"{
              Name: 'Serialize All The Things',
              Date: '2014-06-03'
            }";

        var s = JsonConvert.DeserializeObject<Session>(j, new JsonSerializerSettings());
        // Name = Serialize All The Things
        // Date = Tuesday, 3 June 2014

        Assert.Equal("Serialize All The Things", s.Name);
    }

    [Fact]
    public void DeserializationBasics2()
    {
        var s = new Session
        {
            Date = new DateTime(2014, 6, 4)
        };

        var j = @"{
              'Name': 'Serialize All The Things'
            }";

        JsonConvert.PopulateObject(j, s);
        // Name = Serialize All The Things
        // Date = Tuesday, 3 June 2014
    }

    public class City
    {
        public string Name { get; set; }
        public int Population { get; set; }
    }

    public class Employee
    {
        public string Name { get; set; }
    }

    public class Manager : Employee
    {
        public IList<Employee> Reportees { get; set; }
    }

    [Fact]
    public void SerializeReferencesByValue()
    {
        var arnie = new Employee {Name = "Arnie Admin"};
        var mike = new Manager {Name = "Mike Manager"};
        var susan = new Manager {Name = "Susan Supervisor"};

        mike.Reportees = new[] {arnie, susan};
        susan.Reportees = new[] {arnie};

        var json = JsonConvert.SerializeObject(mike, Formatting.Indented);
        // {
        //   "Reportees": [
        //     {
        //       "Name": "Arnie Admin"
        //     },
        //     {
        //       "Reportees": [
        //         {
        //           "Name": "Arnie Admin"
        //         }
        //       ],
        //       "Name": "Susan Supervisor"
        //     }
        //   ],
        //   "Name": "Mike Manager"
        // }

        XUnitAssert.AreEqualNormalized(@"{
  ""Reportees"": [
    {
      ""Name"": ""Arnie Admin""
    },
    {
      ""Reportees"": [
        {
          ""Name"": ""Arnie Admin""
        }
      ],
      ""Name"": ""Susan Supervisor""
    }
  ],
  ""Name"": ""Mike Manager""
}", json);
    }

    [Fact]
    public void SerializeReferencesWithMetadata()
    {
        var arnie = new Employee {Name = "Arnie Admin"};
        var mike = new Manager {Name = "Mike Manager"};
        var susan = new Manager {Name = "Susan Supervisor"};

        mike.Reportees = new[] {arnie, susan};
        susan.Reportees = new[] {arnie};

        var json = JsonConvert.SerializeObject(mike, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Objects,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        });
        // {
        //   "$id": "1",
        //   "$type": "YourNamespace.Manager, YourAssembly",
        //   "Name": "Mike Manager",
        //   "Reportees": [
        //     {
        //       "$id": "2",
        //       "$type": "YourNamespace.Employee, YourAssembly",
        //       "Name": "Arnie Admin"
        //     },
        //     {
        //       "$id": "3",
        //       "$type": "YourNamespace.Manager, YourAssembly",
        //       "Name": "Susan Supervisor",
        //       "Reportees": [
        //         {
        //           "$ref": "2"
        //         }
        //       ]
        //     }
        //   ]
        // }

        XUnitAssert.AreEqualNormalized(@"{
  ""$id"": ""1"",
  ""$type"": ""DemoTests+Manager, Argon.Tests"",
  ""Reportees"": [
    {
      ""$id"": ""2"",
      ""$type"": ""DemoTests+Employee, Argon.Tests"",
      ""Name"": ""Arnie Admin""
    },
    {
      ""$id"": ""3"",
      ""$type"": ""DemoTests+Manager, Argon.Tests"",
      ""Reportees"": [
        {
          ""$ref"": ""2""
        }
      ],
      ""Name"": ""Susan Supervisor""
    }
  ],
  ""Name"": ""Mike Manager""
}", json);
    }

    [Fact]
    public void RoundtripTypesAndReferences()
    {
        var json = @"{
  '$id': '1',
  '$type': 'DemoTests+Manager, Argon.Tests',
  'Reportees': [
    {
      '$id': '2',
      '$type': 'DemoTests+Employee, Argon.Tests',
      'Name': 'Arnie Admin'
    },
    {
      '$id': '3',
      '$type': 'DemoTests+Manager, Argon.Tests',
      'Reportees': [
        {
          '$ref': '2'
        }
      ],
      'Name': 'Susan Supervisor'
    }
  ],
  'Name': 'Mike Manager'
}";

        var e = JsonConvert.DeserializeObject<Employee>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        });
        // Name = Mike Manager
        // Reportees = Arnie Admin, Susan Supervisor

        var mike = (Manager) e;
        var susan = (Manager) mike.Reportees[1];

        ReferenceEquals(mike.Reportees[0], susan.Reportees[0]);
        // true

        Assert.True(ReferenceEquals(mike.Reportees[0], susan.Reportees[0]));
    }

    public class House
    {
        public string StreetAddress { get; set; }
        public DateTime BuildDate { get; set; }
        public int Bedrooms { get; set; }
        public decimal FloorArea { get; set; }
    }

    public class House1
    {
        public string StreetAddress { get; set; }

        [JsonIgnore] public int Bedrooms { get; set; }

        [JsonIgnore] public decimal FloorArea { get; set; }

        [JsonIgnore] public DateTime BuildDate { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class House3
    {
        [JsonProperty] public string StreetAddress { get; set; }

        public int Bedrooms { get; set; }
        public decimal FloorArea { get; set; }
        public DateTime BuildDate { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class House2
    {
        [JsonProperty("address")] public string StreetAddress { get; set; }

        public int Bedrooms { get; set; }
        public decimal FloorArea { get; set; }
        public DateTime BuildDate { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class House4
    {
        [JsonProperty("address", Order = 2)] public string StreetAddress { get; set; }

        public int Bedrooms { get; set; }
        public decimal FloorArea { get; set; }

        [JsonProperty("buildDate", Order = 1)] public DateTime BuildDate { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class House5
    {
        [JsonProperty("address", Order = 2)] public string StreetAddress { get; set; }

        public int Bedrooms { get; set; }
        public decimal FloorArea { get; set; }

        [JsonProperty("buildDate", Order = 1)] public DateTime BuildDate { get; set; }
    }

    [Fact]
    public void SerializeAttributes()
    {
        var house = new House3
        {
            StreetAddress = "221B Baker Street",
            Bedrooms = 2,
            FloorArea = 100m,
            BuildDate = new DateTime(1890, 1, 1)
        };

        var json = JsonConvert.SerializeObject(house, Formatting.Indented);
        // {
        //   "StreetAddress": "221B Baker Street",
        //   "Bedrooms": 2,
        //   "FloorArea": 100.0,
        //   "BuildDate": "1890-01-01T00:00:00"
        // }

        // {
        //   "StreetAddress": "221B Baker Street"
        // }

        // {
        //   "address": "221B Baker Street"
        // }

        // {
        //   "buildDate": "1890-01-01T00:00:00",
        //   "address": "221B Baker Street"
        // }

        // {
        //   "buildDate": new Date(-2524568400000),
        //   "address": "221B Baker Street"
        // }
    }

    [Fact]
    public void MergeJson()
    {
        var o1 = JObject.Parse(@"{
              'FirstName': 'John',
              'LastName': 'Smith',
              'Enabled': false,
              'Roles': [ 'User' ]
            }");
        var o2 = JObject.Parse(@"{
              'Enabled': true,
              'Roles': [ 'User', 'Admin' ]
            }");

        o1.Merge(o2, new JsonMergeSettings
        {
            // union arrays together to avoid duplicates
            MergeArrayHandling = MergeArrayHandling.Union
        });

        var json = o1.ToString();
        // {
        //   "FirstName": "John",
        //   "LastName": "Smith",
        //   "Enabled": true,
        //   "Roles": [
        //     "User",
        //     "Admin"
        //   ]
        // }

        XUnitAssert.AreEqualNormalized(@"{
  ""FirstName"": ""John"",
  ""LastName"": ""Smith"",
  ""Enabled"": true,
  ""Roles"": [
    ""User"",
    ""Admin""
  ]
}", json);
    }

    [Fact]
    public void ArrayPooling()
    {
        IList<int> value;

        var serializer = new JsonSerializer();
        using (var reader = new JsonTextReader(new StringReader(@"[1,2,3,4]")))
        {
            reader.ArrayPool = JsonArrayPool.Instance;

            value = serializer.Deserialize<IList<int>>(reader);
        }

        Assert.Equal(4, value.Count);
    }
}

public class JsonArrayPool : IArrayPool<char>
{
    public static readonly JsonArrayPool Instance = new();

    public char[] Rent(int minimumLength)
    {
        // use System.Buffers shared pool
        return ArrayPool<char>.Shared.Rent(minimumLength);
    }

    public void Return(char[] array)
    {
        // use System.Buffers shared pool
        ArrayPool<char>.Shared.Return(array);
    }
}