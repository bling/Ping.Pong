using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PingPong.Core;

namespace PingPong.OAuth
{
    /// <summary>represents query parameter(Key and Value)</summary>
    public class Parameter
    {
        public string Key { get; private set; }
        public string Value { get; private set; }

        public Parameter(string key, object value)
        {
            Enforce.NotNull(key, "key");
            Enforce.NotNull(value, "value");

            Key = key;
            Value = value.ToString();
        }

        /// <summary>UrlEncode(Key)=UrlEncode(Value)</summary>
        public override string ToString()
        {
            return Key.UrlEncode() + "=" + Value.UrlEncode();
        }
    }

    /// <summary>represents query parameter(Key and Value) collection</summary>
    public class ParameterCollection : IEnumerable<Parameter>
    {
        private readonly List<Parameter> list = new List<Parameter>();

        public void Add(Parameter parameter)
        {
            Enforce.NotNull(parameter, "parameter");
            list.Add(parameter);
        }

        public void Add(IEnumerable<Parameter> parameters)
        {
            Enforce.NotNull(parameters, "parameters");
            list.AddRange(parameters);
        }

        public void Add(string key, object value)
        {
            list.Add(new Parameter(key, value));
        }

        public IEnumerator<Parameter> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static class ParametersExtension
    {
        /// <summary>convert urlencoded querystring</summary>
        public static string ToQueryParameter(this IEnumerable<Parameter> parameters)
        {
            return parameters.Select(p => p.ToString()).ToString("&");
        }
    }
}