using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using InitialArrange;

namespace User
{
    class UnitTest
    {
        static int bgColor = 255;
        static int fontSize;
        static string fontName;
        static int resultCount; //解数
        static int grid;
        static double time;
        static string siteCsv;
        static string zoneCsv;
        static string locationCsv;
        static void Main(string[] args)
        {
            XmlParser parser = new XmlParser(@"../Configuration/config1.xml");

            //MainAsync(args).GetAwaiter().GetResult();
            Console.ReadLine();
        }

        //FrameWork 4.8 需要这个，core2.1 以上不需要。Gurobi 100支持 .NET core2 
        static async Task MainAsync(string[] args)
        {
            using (FileStream fileStream = new FileStream(@"../Configuration/config1.xml", FileMode.Open, FileAccess.Read))
            {
                await TestReader(fileStream);
            }
            Console.WriteLine(fontName);
            Console.ReadLine();
        }
        /// <summary>
        ///  by XmlReader
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static async Task TestReader(Stream stream)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.Async = true;

            using (XmlReader reader = XmlReader.Create(stream, settings))
            {
                bool start = false;
                while (await reader.ReadAsync())
                {
                    if (reader.Name == "configuration")
                        start = !start;
                    if (!start)
                        continue;

                    if (reader.NodeType != XmlNodeType.Element & reader.NodeType != XmlNodeType.EndElement)
                        continue;

                    //visualization
                    if (reader.Name == "visulization")
                    {
                        while (await reader.ReadAsync())
                        {

                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                switch (reader.Name)
                                {
                                    case "backgroundColor":
                                        Console.WriteLine(reader.Name);
                                        bgColor = reader.ReadElementContentAsInt();//这里在向后读
                                        break;//退出switch 
                                    case "font":
                                        Console.WriteLine(reader.Name);
                                        fontSize = int.Parse(reader.GetAttribute("size"));
                                        fontName = reader.GetAttribute("font");
                                        break;
                                }
                            }

                            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "visulization")
                                break;
                        }
                        Console.WriteLine("The end of " + reader.Name);
                        continue;
                    }


                    //basic
                    if (reader.Name == "basic")
                    {
                        while (await reader.ReadAsync())
                        {
                            if (reader.NodeType != XmlNodeType.Element)
                                continue;
                            switch (reader.Name)
                            {
                                case "grid":
                                    grid = reader.ReadElementContentAsInt();
                                    break;//退出switch 
                                case "resultCount":
                                    resultCount = reader.ReadElementContentAsInt();
                                    break;
                                case "time":
                                    time = reader.ReadElementContentAsDouble();
                                    break;
                            }
                        }
                        continue;
                    }
                    // filepath
                    if (reader.Name == "filepath")
                    {
                        while (await reader.ReadAsync())
                        {
                            if (reader.NodeType != XmlNodeType.Element)
                                continue;
                            switch (reader.Name)
                            {
                                case "zoneFile":
                                    zoneCsv = reader.ReadElementContentAsString();
                                    break;
                                case "siteFile":
                                    siteCsv = reader.ReadElementContentAsString();
                                    break;
                                case "locationFile":
                                    locationCsv = reader.ReadElementContentAsString();
                                    break;
                            }
                            break;
                        }
                        //if (reader.Name == "Shaoe")
                        //{
                        //    while (await reader.ReadAsync())
                        //    {

                        //    }
                        //    break;
                        //}
                        //if (reader.Name == "Topology")
                        //{
                        //    while (await reader.ReadAsync())
                        //    {

                        //    }
                        //    break;
                        //}
                        //if (reader.Name == "StructureElements")
                        //{
                        //    while (await reader.ReadAsync())
                        //    {

                        //    }
                        //    break;
                        //}

                    }

                }
            }
        }
    }
}
