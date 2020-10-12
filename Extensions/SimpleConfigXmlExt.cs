using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WaveFunctionCollapse.Configs;

namespace WaveFunctionCollapse.Extensions
{
    public static class SimpleConfigXmlExt
    {
        public static XElement LoadSampleXml(this SimpleTiledConfig config)
        {
            return XDocument.Load($"samples/{config.name}/data.xml").Root;
        }
        
        public static List<string> LoadTileSubset(this SimpleTiledConfig config, XElement xroot)
        {
            if (config.subset != null)
            {
                var xsubset = xroot.Element("subsets").Elements("subset").FirstOrDefault(x => x.Get<string>("name") == config.subset);
                if (xsubset == null)
                {
                    Console.WriteLine($"ERROR: subset {config.subset} is not found");
                }
                else
                {
                    return xsubset.Elements("tile").Select(x => x.Get<string>("name")).ToList();
                }
            }

            return null;
        }
    }
}