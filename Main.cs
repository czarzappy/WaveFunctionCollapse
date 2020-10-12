/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Diagnostics;
using System.Xml.Linq;
using WaveFunctionCollapse.Configs;
using WaveFunctionCollapse.Extensions;
using WaveFunctionCollapse.Factories;
using WaveFunctionCollapse.Sim;

namespace WaveFunctionCollapse
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Stopwatch sw = Stopwatch.StartNew();

            Random random = new Random();
            XDocument xdoc = XDocument.Load(args[0]);

            int counter = 1;
            foreach (XElement xelem in xdoc.Root.Elements("overlapping", "simpletiled"))
            {
                Model model;
                string name = xelem.Get<string>("name");
                Console.WriteLine($"< {name}");

                switch (xelem.Name.ToString())
                {
                    case "simpletiled":
                        var config = SimpleTiledConfigFactory.FromXmlNode(xelem);
                        model = new SimpleTiledModel(config);
                        break;
                    // case 
                        // var config = OverlappingConfigFactory.FromXmlNode(xelem);
                        // model = new OverlappingModel(config);
                    default:
                        continue;
                }

                for (int screenshotId = 0; screenshotId < xelem.Get("screenshots", 2); screenshotId++)
                {
                    for (int k = 0; k < 10; k++)
                    {
                        Console.Write("> ");
                        int seed = random.Next();
                        bool finished = model.Run(seed, xelem.Get("limit", 0));
                        if (finished)
                        {
                            Console.WriteLine("DONE");

                            model.Graphics().Save($"{counter} {name} {screenshotId}.png");
                            var tiledModel = model as SimpleTiledModel;
                            if (tiledModel != null && xelem.Get("textOutput", false))
                            {
                                System.IO.File.WriteAllText($"{counter} {name} {screenshotId}.txt", tiledModel.TextOutput());
                            }

                            break;
                        }

                        Console.WriteLine("CONTRADICTION");
                    }
                }

                counter++;
            }

            Console.WriteLine($"time = {sw.ElapsedMilliseconds}");
        }
    }
}
