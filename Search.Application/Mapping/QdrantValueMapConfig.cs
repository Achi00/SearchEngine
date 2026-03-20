using Mapster;
using Qdrant.Client.Grpc;

namespace Search.Application.Mapping
{
    // No mapster, manual mapping
    // Dictionary<string, object> -> Dictionary<string, Value> (Qdrant.Value)
    public static class QdrantValueMapConfig
    {
        private static readonly Dictionary<Type, Func<object, Value>> _map = new()
        {
            [typeof(string)] = v => new Value { StringValue = (string)v },
            [typeof(int)] = v => new Value { IntegerValue = (int)v },
            [typeof(long)] = v => new Value { IntegerValue = (long)v },
            [typeof(float)] = v => new Value { DoubleValue = (float)v },
            [typeof(double)] = v => new Value { DoubleValue = (double)v },
            [typeof(bool)] = v => new Value { BoolValue = (bool)v },
            // avoids unboxing issue, type should be same decimal -> boxing/unboxing -> decimal
            [typeof(decimal)] = v => new Value { DoubleValue = (double)(decimal)v },
            [typeof(DateTime)] = v => new Value { StringValue = ((DateTime)v).ToString("O") }
        };
        public static Value ToValue(object value)
        {
            if (value == null)
            {
                return new Value { NullValue = NullValue.NullValue };
            }

            // in case qdrant returned collections
            if (value is System.Collections.IEnumerable enumerable && value is not string)
            {
                var list = new List<Value>();

                foreach (var item in enumerable)
                {
                    list.Add(ToValue(item!));
                }

                return new Value
                {
                    ListValue = new ListValue { Values = { list } }
                };
            }

            // for nullable types
            var type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();

            if (_map.TryGetValue(type, out var converter))
            {
                return converter(value);
            }

            throw new NotSupportedException($"Unsupported payload type: {value.GetType()}");
        }

        // reverse mapping, from search results
        public static object FromValue(Value value)
        {
            return value.KindCase switch
            {
                Value.KindOneofCase.StringValue => value.StringValue,
                Value.KindOneofCase.IntegerValue => value.IntegerValue,
                Value.KindOneofCase.DoubleValue => value.DoubleValue,
                Value.KindOneofCase.BoolValue => value.BoolValue,

                Value.KindOneofCase.ListValue => value.ListValue.Values
                    .Select(FromValue)
                    .ToList(),

                Value.KindOneofCase.StructValue => value.StructValue.Fields
                    .ToDictionary(
                        kv => kv.Key,
                        kv => FromValue(kv.Value)),

                Value.KindOneofCase.NullValue => null!,

                _ => throw new NotSupportedException(
                    $"Unsupported value type: {value.KindCase}")
            };
        }

        public static Dictionary<string, Value> ToPayload(
            Dictionary<string, object>? payload)
        {
            if (payload == null)
                return new();

            return payload.ToDictionary(
                kv => kv.Key,
                kv => ToValue(kv.Value));
        }
    }
}
