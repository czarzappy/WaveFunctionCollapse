using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;

namespace WaveFunctionCollapse.Extensions
{
    public static class XmlExt
    {
        public static T Get<T>(this XElement xelem, string attribute, T defaultT = default)
        {
            XAttribute a = xelem.Attribute(attribute);
            return a == null ? defaultT : (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(a.Value);
        }

        public static IEnumerable<XElement> Elements(this XElement xelement, params string[] names)
        {
            return xelement.Elements().Where(e => names.Any(n => n == e.Name));
        }
    }
}