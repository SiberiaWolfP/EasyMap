using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EasyMap
{
    [XmlRoot("MyDictionary")]
    public class MyDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement) { return; }

            reader.Read();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                object key = reader.GetAttribute("Title");
                object value = reader.GetAttribute("Value");
                if (key != null)
                {
                    JToken token;
                    try
                    {
                        token = JObject.Parse((string)value);
                    }
                    catch (Exception e)
                    {
                        token = JArray.Parse((string)value);
                    }
                    
                    this.Add((TKey) Convert.ChangeType(key, typeof(TKey)), token.ToObject<TValue>());
                }
                reader.Read();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var key in this.Keys)
            {
                writer.WriteStartElement("Item");
                writer.WriteStartAttribute("Title");
                writer.WriteValue(key);
                writer.WriteEndAttribute();
                writer.WriteAttributeString("Value", JsonConvert.SerializeObject(this[key]));
                writer.WriteEndElement();
            }
        }
    }
}
