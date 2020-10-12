using System.Xml.Linq;
using WaveFunctionCollapse.Configs;
using WaveFunctionCollapse.Extensions;

namespace WaveFunctionCollapse.Factories
{
    public static class SimpleTiledConfigFactory
    {
        public static SimpleTiledConfig FromXmlNode(XElement xelem)
        {
            return new SimpleTiledConfig
            {
                name = xelem.Get<string>("name"),
                subset = xelem.Get<string>("subset"),
                width = xelem.Get("width", 10),
                height = xelem.Get("height", 10),
                periodic = xelem.Get("periodic", false),
                black = xelem.Get("black", false)
            };
        }
    }
}