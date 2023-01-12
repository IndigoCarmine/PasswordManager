using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml;

namespace PasswordManager
{
    /// <summary>
    /// 拝借したもの
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable where TKey : notnull
    {
        public XmlSchema? GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            var serializer = new XmlSerializer(typeof(KeyValueItem));

            reader.Read();
            if (reader.IsEmptyElement)
                return;

            try
            {
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    // これがないと下でぬるりが出る時がある
                    if (!serializer.CanDeserialize(reader))
                        return;
                    else
                    {
                        var item = serializer.Deserialize(reader) as KeyValueItem; // 従来はここでnullになるときがあった
                        this.Add(item!.Key!, item.Value!);
                    }
                }
            }
            finally
            {
                reader.Read();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, string.Empty);

            var serializer = new XmlSerializer(typeof(KeyValueItem));

            foreach (var item in this.Keys.Select(key => new KeyValueItem(key, this[key])))
            {
                serializer.Serialize(writer, item, ns);
            }
        }

        public class KeyValueItem
        {
            public TKey? Key { get; set; }
            public TValue? Value { get; set; }

            public KeyValueItem(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }

            public KeyValueItem()
            {
            }
        }
    }

}
