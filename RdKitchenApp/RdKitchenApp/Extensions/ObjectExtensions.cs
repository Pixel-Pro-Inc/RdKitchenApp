using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace RdKitchenApp.Extensions
{
    public static class ObjectExtensions
    {
        public static T ToObject<T>(this IDictionary<string, object> source)
        where T : class, new()
        {
            var someObject = new T();
            var someObjectType = someObject.GetType();

            foreach (var item in source)
            {
                if (item.Value == null)
                    continue;

                someObjectType
                         .GetProperty(item.Key)
                         .SetValue(someObject, item.Value, null);
            }

            return someObject;
        }

        public static IDictionary<string, object> AsDictionary(this object source, BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
        {
              return source.GetType().GetProperties(bindingAttr).ToDictionary
            (
                propInfo => propInfo.Name,
                propInfo => propInfo.GetValue(source, null)
            );

        }

        public static byte[] ToByteArray<T>(this T obj)
        {
            //If crash still happens address this one
            var data = JsonConvert.SerializeObject(obj);

            if (data == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, data);
                return ms.ToArray();
            }
        }

        public async static Task<T> FromByteArray<T>(this byte[] data)
        {
            if (data == null)
                return default(T);

            try
            {
                var obj = Encoding.UTF8.GetString(data);

                var _object = await JsonConvert.DeserializeObjectAsync<T>(obj);
                return _object;
            }
            catch
            {
                //Incase Json Deserializer Sends Back Weird String We avoid crash by sending default back
                return default(T);
            }
        }
    }
}
