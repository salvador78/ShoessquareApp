using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Grand.Plugin.Payments.Stripe.Models
{
    [XmlRoot(ElementName = "item")]
    public class Item
    {
        [XmlElement(ElementName = "key")]
        public string Key { get; set; }
        [XmlElement(ElementName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "DictionarySerializer")]
    public class DictionarySerializer
    {
        [XmlElement(ElementName = "item")]
        public Item Item { get; set; }
    }
}
