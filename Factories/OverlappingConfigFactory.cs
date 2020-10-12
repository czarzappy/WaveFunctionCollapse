using System.Xml.Linq;
using WaveFunctionCollapse.Configs;
using WaveFunctionCollapse.Extensions;

namespace WaveFunctionCollapse.Factories
{
    public static class OverlappingConfigFactory
    {
        public static OverlappingConfig FromXmlNode(XElement xelem)
        {
            return new OverlappingConfig
            {
                name = xelem.Get<string>("name"),
                N = xelem.Get("N", 2), 
                width = xelem.Get("width", 48), 
                height = xelem.Get("height", 48),
                periodicInput = xelem.Get("periodicInput", true), 
                periodicOutput = xelem.Get("periodic", false), 
                symmetry = xelem.Get("symmetry", 8), 
                ground = xelem.Get("ground", 0)
            };
        }
    }
}