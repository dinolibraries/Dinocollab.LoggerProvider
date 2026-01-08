using Newtonsoft.Json;
using QuestDB.Senders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Dinocollab.LoggerProvider.QuestDB
{
    internal static class HelperExtension
    {
        private static object CreateInstance(Type type, object? value)
        {
            var constructor = typeof(ReadOnlySpan<>).MakeGenericType(type);
            return Activator.CreateInstance(constructor, value)!;
        }

        private static ISender SetValue(this ISender sender, string columnName, object? value)
        {
            if (value == null)
            {
                value = DBNull.Value;
            }
            var type = value.GetType();
            //check is TypeCode
            if (type.IsEnum)
            {
                return sender.Column(columnName, Convert.ToInt32(value));
            }
            else if (type == typeof(Guid))
            {
                return sender.Column(columnName, value.ToString());
            }
            else if (type.IsArray || value is IEnumerable)
            {
                return sender.Column(columnName, JsonConvert.SerializeObject(value));
            }
            else if (type == typeof(byte[]))
            {
                return sender.Column(columnName, Convert.ToBase64String((byte[])value));
            }
            else if (!type.IsClass)
            {
                var typeCode = Type.GetTypeCode(type);

                switch (typeCode)
                {
                    case TypeCode.Empty:
                        break;
                    case TypeCode.Object:
                        return sender.Column(columnName, JsonConvert.SerializeObject(value));
                    case TypeCode.DBNull:
                        break;
                    case TypeCode.Boolean:
                        return sender.Column(columnName, Convert.ToBoolean(value));
                    case TypeCode.Char:
                        return sender.Column(columnName, Convert.ToChar(value));
                    case TypeCode.SByte:
                        return sender.Column(columnName, Convert.ToSByte(value));
                    case TypeCode.Byte:
                        return sender.Column(columnName, Convert.ToByte(value));
                    case TypeCode.Int16:
                        return sender.Column(columnName, Convert.ToInt16(value));
                    case TypeCode.UInt16:
                        return sender.Column(columnName, Convert.ToUInt16(value));
                    case TypeCode.Int32:
                        return sender.Column(columnName, Convert.ToInt32(value));
                    case TypeCode.UInt32:
                        return sender.Column(columnName, Convert.ToUInt32(value));
                    case TypeCode.Int64:
                        return sender.Column(columnName, Convert.ToInt64(value));
                    case TypeCode.UInt64:
                        return sender.Column(columnName, Convert.ToDouble(value));
                    case TypeCode.Single:
                        return sender.Column(columnName, Convert.ToSingle(value));
                    case TypeCode.Double:
                        return sender.Column(columnName, Convert.ToDouble(value));
                    case TypeCode.Decimal:
                        return sender.Column(columnName, Convert.ToDecimal(value));
                    case TypeCode.DateTime:
                        return sender.Column(columnName, Convert.ToDateTime(value));
                    case TypeCode.String:
                        return sender.Column(columnName, value.ToString());
                    default:
                        return sender.Column(columnName, value.ToString());
                }
            }
            return sender;
        }
        public static ISender ToConvert<T>(this ISender sender, T data)
        {
            var properties = typeof(T).GetProperties();

            //ignord timestampe
            var timeProp = properties.FirstOrDefault(x => Attribute.IsDefined(x, typeof(TimestampAttribute)));

            if (timeProp == null)
            {
                timeProp = properties.First(x => x.PropertyType == typeof(DateTime));
            }

            var propFiltered = properties.Where(x => x != timeProp);

            foreach (var prop in propFiltered)
            {
                var propName = prop.Name;
                var propValue = prop.GetValue(data);
                if (propValue == null)
                {
                    continue; // Skip null values
                }
                sender = sender.SetValue(propName, propValue);
            }
            return sender;
        }
        public static string GetTableName(this Type type)
        {
            var tableName = type.Name; // Use the provided type instead of typeof(T)
            var tableAttr = type.GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault() as TableAttribute;
            if (tableAttr != null && !string.IsNullOrEmpty(tableAttr.Name))
            {
                tableName = tableAttr.Name;
            }
            return tableName;
        }
        public static async Task<string> CreateTableAsync<T>(this string apiUrl, string? tablename = null, HttpClient? httpClient = null)
        {
            var createTableSql = CreateTableSql<T>(tablename);
            //using http rest api to create table
            /* curl -G \
            --data-urlencode "query=SELECT timestamp, price FROM trades LIMIT 2;" \
            --data-urlencode "count=true" \
            http://localhost:9000/exec */
            httpClient ??= new HttpClient();
            var requestUrl = $"{apiUrl}/exec?query={Uri.EscapeDataString(createTableSql)}";
            var response = await httpClient.GetAsync(requestUrl);
            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to alter TTL: {message}");
            }
            return createTableSql;
        }
        public static string CreateTableSql<T>(string? tablename = null)
        {
            var properties = typeof(T).GetProperties();
            var columns = new List<string>();

            var tableName = tablename ?? typeof(T).GetTableName();

            foreach (var prop in properties)
            {
                if (Attribute.IsDefined(prop, typeof(IgnoreDataMemberAttribute)))
                    continue;

                var propName = prop.Name;

                // 👇 UNWRAP Nullable<T>
                var type = Nullable.GetUnderlyingType(prop.PropertyType)
                           ?? prop.PropertyType;

                string sqlType = type switch
                {
                    Type t when t == typeof(string) => "STRING",
                    Type t when t == typeof(int) => "INT",
                    Type t when t == typeof(long) => "LONG",
                    Type t when t == typeof(bool) => "BOOLEAN",
                    Type t when t == typeof(DateTime) => "TIMESTAMP",
                    Type t when t == typeof(double) => "DOUBLE",
                    Type t when t == typeof(float) => "FLOAT",
                    _ => "STRING"
                };

                columns.Add($"{propName} {sqlType}");
            }

            var columnsSql = string.Join(",\n    ", columns);

            //find table time column with attribute [Timestamp]
            var timeColProp = properties.FirstOrDefault(x => Attribute.IsDefined(x, typeof(TimestampAttribute)));

            //find time first column
            if (timeColProp == null)
            {
                timeColProp = properties.First(x => x.PropertyType == typeof(DateTime));
            }

            return $"""
                    CREATE TABLE IF NOT EXISTS {tableName} (
                        {columnsSql}
                    )
                    TIMESTAMP({timeColProp.Name})
                    PARTITION BY DAY;
            """;
        }

        public static async Task AlterTTLAsync<T>(this string apiUrl, int ttlDays, HttpClient? httpClient = null)
        {
            var alterTableSql = $"ALTER TABLE {typeof(T).GetTableName()} SET TTL {ttlDays} DAYS;";
            if (ttlDays <= 0)
            {
                alterTableSql = $"ALTER TABLE {typeof(T).GetTableName()} SET TTL NONE;";
            }
            //using http rest api to create table
            /* curl -G \
            --data-urlencode "query=SELECT timestamp, price FROM trades LIMIT 2;" \
            --data-urlencode "count=true" \
            http://localhost:9000/exec */
            httpClient ??= new HttpClient();
            var requestUrl = $"{apiUrl}/exec?query={Uri.EscapeDataString(alterTableSql)}";
            var response = await httpClient.GetAsync(requestUrl);
            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to alter TTL: {message}");
            }
        }
    }
}
