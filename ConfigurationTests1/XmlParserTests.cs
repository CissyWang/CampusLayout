using Microsoft.VisualStudio.TestTools.UnitTesting;
using Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigurationTests1
{
    [TestClass()]
    public class XmlParserTests
    {
        [TestMethod()]
        public void XmlParserTest()
        {
            string expected = "../../南工职大/应用/export1.2.csv";
            XmlParser parser = new XmlParser("E:/grasshopper_C#/Campus/Configuration/config1.xml");
            string actual = parser.Filepaths[0];
            
            Assert.AreEqual(actual, expected);
        }

    }
}