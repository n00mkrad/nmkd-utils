using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace NmkdUtils
{
    public static class JsonExtensions
    {
        /// <summary> Deserialize JSON <paramref name="s"/> into an object. </summary>
        public static T FromJson<T>(this string s)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(s))
                    return default;

                return JsonConvert.DeserializeObject<T>(s);
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Failed to deserialize");
                return default;
            }
        }

        /// <summary> Deserialize JSON using custom settings. </summary>
        public static T FromJson<T>(this string s, NullValueHandling nullHandling, DefaultValueHandling defHandling, IContractResolver contractResolver = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(s))
                    return default;

                var settings = new JsonSerializerSettings();

                settings.NullValueHandling = nullHandling;
                settings.DefaultValueHandling = defHandling;

                if (contractResolver != null)
                    settings.ContractResolver = contractResolver;

                return JsonConvert.DeserializeObject<T>(s, settings);
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Failed to deserialize");
                return default;
            }
        }

        /// <summary> Serialize an object to JSON. </summary>
        public static string ToJson(this object o, bool indent = false, bool ignoreErrors = true)
        {
            var settings = new JsonSerializerSettings();

            if (ignoreErrors)
                settings.Error = (s, e) => { e.ErrorContext.Handled = true; };

            // Serialize enums as strings.
            settings.Converters.Add(new StringEnumConverter());

            return JsonConvert.SerializeObject(o, indent ? Formatting.Indented : Formatting.None, settings);
        }
    }
}
