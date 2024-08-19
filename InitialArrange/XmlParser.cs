//从xml文件中读取信息并配置给calculator
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace InitialArrange
{
    public class XmlParser
    {
        private XDocument _doc;
        //static 用于类本身而不是对象

        public XmlParser(string xmlFilePath)
        {
            try
            {
                // Load the XML document
                _doc = XDocument.Load(xmlFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
        public int Unit { get => int.Parse(_doc.Root.Element("basic").Element("unit").Value); }


        /// filepath路径
        public string[] Filepaths { get => _doc.Root.Element("filepath").Elements().Select(e => e.Value.Trim()).ToArray(); }

        /// weight 目标权重

        public void BasicSettings(Calculator cal)
        {
            cal.unit = Unit;
            cal.isInteger = int.Parse(_doc.Root.Element("basic").Element("isInteger").Value);
            cal.resultCount = int.Parse(_doc.Root.Element("basic").Element("resultCount").Value);
            cal.time = double.Parse(_doc.Root.Element("basic").Element("time").Value);
            cal.poolSearchMode = int.Parse(_doc.Root.Element("basic").Element("searchMode").Value ?? "2");
            cal.weights = _doc.Root.Element("weights").Elements().Select(e => double.Parse(e.Value)).ToArray();
            cal.site = new Site(Filepaths[1]);
            cal.fileName = Filepaths[2];
        }
        public void ShapeSettings(Calculator cal)
        {
            XElement shapeNode = _doc.Root.Element("Shape");
            if (shapeNode != null)
            {
                SetLenToWidth(shapeNode, cal.ZoneVars);
                SetSpacing(shapeNode, cal);
                SetMinLength(shapeNode, cal.Zones);
                SetAreaFloats(shapeNode, cal.Zones);
                SetAreaSep(shapeNode, cal.Zones);
                SetSportsArea(shapeNode, cal);
                SetTotalArea(shapeNode, cal);
            }
        }
        public void TopologySettings(Calculator cal)
        {
            XElement shapeNode = _doc.Root.Element("Topology");
            if (shapeNode != null)
            {
                ParseRoadLinks(shapeNode.Element("RoadLinks"), cal.Zones);
                ParsePointLinks(shapeNode.Element("PointLinks"), cal.Zones);
                ParseZoneLinks(shapeNode.Element("ZoneLinks"), cal);
            }
        }
        public void StructureSettings(Calculator cal)
        {
            XElement shapeNode = _doc.Root.Element("StructureElements");
            if (shapeNode != null)
            {
                //这一组直接使用calculator封装好的方法
                ParseCore(shapeNode.Element("Core"), cal);
                ParseAxes(shapeNode.Element("Axes"), cal);
                ParseGroups(shapeNode.Element("Groups"), cal);
                ParseGrids(shapeNode.Element("Grids"), cal);
            }
        }

        #region private -- shape
        private void SetLenToWidth(XElement Xelement, List<ZoneVar> zoneVars)
        {
            double lenToWidth = double.Parse(Xelement.Element("LenToWidth")?.Value);//
            if (lenToWidth > 0)
            {
                foreach (ZoneVar dv in zoneVars)
                {
                    dv.lenToWidth = lenToWidth;
                }
            }
        }
        private void SetSpacing(XElement Xelement, Calculator cal)
        {
            double spacing = double.Parse(Xelement.Element("Spacing")?.Value);
            if (spacing > 0)
            {
                cal.Spacing = spacing / Unit;
            }
        }
        private void SetMinLength(XElement Xelement, List<IZone> zones)
        {
            var elements = Xelement.Elements("MinLength");
            if (elements.Count() == 0)
                return;
            foreach (XElement e in elements)
            {
                double min = double.Parse(_doc.Root.Element("Shape").Element("MinLength").Value ?? "60");
                if (min <= 0)
                    continue;
                if (e.Attribute("zoneID") != null)
                {
                    int index = int.Parse(e.Attribute("zoneID").Value);
                    zones[index].Length_min = min / Unit;
                    continue;
                }
                foreach (IZone z in zones)
                {
                    z.Length_min = min / Unit;
                }
            }
        }
        private void SetAreaFloats(XElement Xelement, List<IZone> zones)
        {
            var elements = Xelement.Elements("AreaFloats");
            if (elements.Count() == 0)
                return;
            double min = 0, max = 0;
            foreach (XElement e in elements)
            {
                min = double.Parse(e.Attribute("min").Value ?? "1");
                max = double.Parse(e.Attribute("max").Value ?? "0");
                //指定zone
                if (e.Attribute("zoneID") != null)
                {
                    var index = ParseIntArray(e.Attribute("zoneID").Value);
                    foreach (int i in index)
                    {
                        zones[i].area_lim = new Domain(min * zones[i].S, max * zones[i].S);
                    }

                    continue;
                }
                //全部zone
                if (max == 0)
                {
                    //只有下限
                    foreach (IZone d in zones)
                    {
                        d.Site_area = min * d.Site_area;
                    }
                    continue;
                }
                foreach (IZone d in zones)
                {
                    //上下限
                    d.area_lim = new Domain(min * d.S, max * d.S);
                }

            }
        }
        private void SetAreaSep(XElement Xelement, List<IZone> zones)
        {
            var elements = Xelement.Elements("SepArea");
            if (elements.Count() == 0)
                return;
            double k = 0;
            foreach (XElement e in elements)
            {
                k = double.Parse(_doc.Root.Element("Shape").Element("AreaSep")?.Value);
                if (k > 1 || k < 0)
                {
                    continue;
                }
                if (e.Attribute("zoneID") != null)
                {
                    foreach (IZone d in zones)
                    {
                        if (d.Count > 1 && d.Building_area > 0) //户外体育不受影响
                        {
                            foreach (ZoneVar dv in d.zoneVars)
                            {
                                dv.Area_lim.min = k * d.S / d.Count;
                            }
                        }
                    }
                    continue;
                }
                int index = int.Parse(e.Attribute("zoneID").Value);
                if (zones[index].Count > 1)
                {
                    foreach (ZoneVar dv in zones[index].zoneVars)
                    {
                        dv.Area_lim.min = k * zones[index].S / zones[index].Count;
                    }
                }
            }
        }
        private void SetSportsArea(XElement Xelement, Calculator cal)
        {
            var a = Xelement.Elements("SportArea")?.Elements().Select(e => double.Parse(e.Value)).ToArray();
            if (a.Length > 0)
                cal.SportInfo = a;
            else
                cal.SportInfo = new double[2] { 0, 0 };
        }
        private void SetTotalArea(XElement Xelement, Calculator cal)
        {
            string s = Xelement.Element("Density")?.Value;
            if (s == null)
                return;
            double density = double.Parse(s);
            if (density > 0 && density < 1)
            {
                cal.LayoutDensity = density;
            }
            if (_doc.Root.Element("Shape").Element("TotalArea") != null)
            {
                cal.TotalAreaLimit = true;
                cal.TotalArea = double.Parse(_doc.Root.Element("Shape").Element("TotalArea").Value);
            }
        }

        #endregion

        #region private- - ParseStructure
        private void ParseCore(XElement coreElement, Calculator cal)
        {
            if (coreElement != null)
            {
                if (cal.Mode.Length != 0)
                    cal.Mode += ",Core";
                else
                    cal.Mode = "Core";

                cal.core = new IGroup(Unit);
                cal.core.stroke = double.Parse(coreElement.Attribute("stroke")?.Value);
                if (coreElement.Element("Domain") != null)
                {
                    XElement core_domain = coreElement.Element("Domain");
                    var xrange = ParseDoubleArray(core_domain.Attribute("xrange").Value);
                    var yrange = ParseDoubleArray(core_domain.Attribute("yrange").Value);

                    if (core_domain.Attribute("widthDomain") != null & core_domain.Attribute("lengthDomain") != null)
                    {
                        var widthDomain = ParseDoubleArray(core_domain.Attribute("widthDomain").Value);
                        var lengthDomain = ParseDoubleArray(core_domain.Attribute("lengthDomain").Value);
                        cal.core.domain = new Domain[4] { new Domain(xrange), new Domain(yrange), new Domain(widthDomain), new Domain(lengthDomain) };
                    }
                    else
                    {
                        cal.core.domain = new Domain[2] { new Domain(xrange), new Domain(yrange) }; //不限定长宽
                    }
                    cal.core.minimizeWeight = double.Parse(coreElement.Element("minimizeWeight").Value);

                    if (coreElement.Element("insideZone") != null)
                    {
                        bool isOnly = Boolean.Parse(coreElement.Element("insideZone").Attribute("isOnly").Value);
                        bool isAlign = Boolean.Parse(coreElement.Element("insideZone").Attribute("isAlign").Value);
                        string zoneID = coreElement.Element("insideZone").Attribute("zoneID").Value.Trim('{', '}');
                        SetInsideGroup(cal.core, zoneID, isOnly, isAlign, cal);
                    }
                    if (coreElement.Element("outsideZone") != null)
                    {
                        bool isAlign_out = bool.Parse(coreElement.Element("outsideZone").Attribute("isAlign").Value);
                        string zoneID_out = coreElement.Element("outsideZone").Attribute("zoneID").Value.Trim('{', '}');
                        SetOutsideGroup(cal.core, zoneID_out, isAlign_out, cal);
                    }
                }
            }
        }

        private void ParseGroups(XElement groupsElement, Calculator cal)
        {
            if (groupsElement != null)
            {
                if (cal.Mode.Length != 0)
                    cal.Mode += ",Groups";
                else
                    cal.Mode = "Groups";

                if (groupsElement.Elements("Group") != null)
                {
                    cal.groups = new List<IGroup>();
                    foreach (XElement groupXE in groupsElement.Elements("Group"))
                    {
                        int id = int.Parse(groupXE.Attribute("id")?.Value);
                        var minimizeWeight = double.Parse(groupXE.Attribute("minimizeWeight")?.Value);

                        var newGroup = new IGroup(Unit)
                        {
                            index = id,
                            minimizeWeight = minimizeWeight
                        };
                        if (groupXE.Attribute("lenToWidth") != null)
                        {

                            var l2w = double.Parse(groupXE.Attribute("lenToWidth").Value);
                            newGroup.lenToWidth = l2w;
                        }

                        var zones = groupXE.Attribute("zoneID").Value.Trim('{', '}');
                        SetInsideGroup(newGroup, zones, true, false, cal);

                        cal.groups.Add(newGroup);
                    }
                }
            }
        }
        private void SetOutsideGroup(IGroup g, string districtIndex, bool align, Calculator cal)
        {
            g.outsideZones = new List<int>();
            for (int i = 0; i < cal.zones.Count; i++)
            {
                if (districtIndex.Contains("all"))
                {
                    g.outsideZones.Add(i);
                }
                else
                {
                    try
                    {
                        var indexes = districtIndex.Split(',');
                        if (indexes.Contains(i.ToString()))
                            g.outsideZones.Add(i);
                    }
                    catch
                    {
                        Console.WriteLine("Outside选取失败");
                    }
                }
            }
            g.outsideAlign = align;
        }
        private void SetInsideGroup(IGroup g, string zoneIDs, bool only, bool align, Calculator cal)
        {
            g.insideZones = new List<int>();
            for (int i = 0; i < cal.zones.Count; i++)
            {
                if (zoneIDs.Contains("all"))
                {
                    g.insideZones.Add(i);
                }
                else
                {
                    try
                    {
                        var indexes = zoneIDs.Split(',');
                        if (indexes.Contains(i.ToString()))
                            g.insideZones.Add(i);
                    }
                    catch
                    {
                        Console.WriteLine("CoreInside选取失败");
                    }
                }
            }
            g.insideOnly = only;
            g.insideAlign = align;
        }
        private void ParseGrids(XElement gridsElement, Calculator cal)
        {
            if (gridsElement != null)
            {
                if (cal.Mode.Length != 0)
                    cal.Mode += ",Grids";
                else
                    cal.Mode = "Grids";
                cal.gridVars = new List<LinearVar>();
                if (gridsElement.Elements("xGrid") != null)
                    ParseGridControl(gridsElement.Elements("xGrid"), 1, cal);
                if (gridsElement.Elements("yGrid") != null)
                    ParseGridControl(gridsElement.Elements("yGrid"), 0, cal);
            }
        }

        private void ParseGridControl(IEnumerable<XElement> elements, int dir, Calculator cal)
        {
            foreach (XElement xgridXE in elements)
            {
                int id = int.Parse(xgridXE.Attribute("id")?.Value);
                var stroke = float.Parse(xgridXE.Attribute("stroke")?.Value);
                var xrange = ParseDoubleArray(xgridXE.Element("Domain").Attribute("xrange")?.Value);
                var yrange = ParseDoubleArray(xgridXE.Element("Domain").Attribute("yrange")?.Value);
                var range = new Domain[2] { new Domain(xrange[0], xrange[1]), new Domain(yrange[0], yrange[1]) };
                var xgrid = new LinearVar(id, dir, stroke, range, Unit);

                if (xgridXE.Element("ControlZones") != null)
                {
                    var zones = ParseIntArray(xgridXE.Element("ControlZones").Attribute("zoneIDs").Value);
                    var side = xgridXE.Element("ControlZones").Attribute("side").Value;
                    int sideN;
                    if (side == "top" | side == "right")
                        sideN = 1;
                    else
                        sideN = 0;
                    SetGridControl(xgrid, zones, sideN, cal);
                }
                cal.gridVars.Add(xgrid);

            }
        }
        private void SetGridControl(LinearVar gridVar, int[] zones, int side, Calculator cal)
        {
            List<int> op = new List<int>();
            List<int> ad = new List<int>();
            for (int i = 0; i < cal.zones.Count; i++)
            {
                if (!zones.Contains(i))
                {
                    op.Add(i);
                }
                else { ad.Add(i); }
            }

            if (side == 0)
            {
                gridVar.zonesLorB = ad;
                gridVar.zonesRorT = op;
            }
            else if (side == 1)
            {
                gridVar.zonesLorB = op;
                gridVar.zonesRorT = ad;
            }

        }
        private void ParseAxes(XElement axesElement, Calculator cal)
        {
            if (axesElement != null)
            {
                if (cal.Mode.Length != 0)
                    cal.Mode += ",Axes";
                else
                    cal.Mode = "Axes";

                if (axesElement.Elements("Axis") != null)
                {
                    foreach (XElement axisXE in axesElement.Elements("Axis"))
                    {
                        var startPt = ParseFloatArray(axisXE.Attribute("startPt").Value);
                        var endPt = ParseFloatArray(axisXE.Attribute("endPt").Value);
                        var axis = new Axis(startPt[0], startPt[1], endPt[0], endPt[1]);
                        axis.width = double.Parse(axisXE.Attribute("width")?.Value);
                        if (axisXE.Element("asRealAxis") != null)
                        {
                            axis.asideZones = ParseIntArray(axisXE.Element("asRealAxis").Attribute("zoneID").Value);
                        }
                        if (axisXE.Element("asAbstractAxis") != null)
                        {
                            axis.onZones = ParseIntArray(axisXE.Element("assAbstractAxis").Attribute("zoneID").Value);
                            axis.buffer = double.Parse(axisXE.Element("assAbstractAxis").Attribute("buffer")?.Value);
                            axis.isCenter = bool.Parse(axisXE.Element("assAbstractAxis").Attribute("onCenter")?.Value);
                        }
                        cal.axes.Add(axis);
                    }
                }
            }
        }
        #endregion

        #region private -- ParseLinks
        private void ParseZoneLinks(XElement zoneLinksElement, Calculator cal)
        {
            if (zoneLinksElement == null)
                return;
            foreach (XElement zoneLinkElement in zoneLinksElement.Elements("ZoneLink"))
            {
                string type = zoneLinkElement.Attribute("type")?.Value;
                int[] zoneIDs = ParseIntArray(zoneLinkElement.Attribute("zoneID").Value);
                switch (type)
                {
                    case "near":
                        cal.ZoneLink.Add(zoneIDs);
                        break;
                    case "subZoneAway":
                        foreach (int e in zoneIDs)
                        {
                            cal.ZoneDist.Add(e);
                        }
                        break;
                }

            }
        }

        private void ParseRoadLinks(XElement roadLinksElement, List<IZone> zones)
        {
            if (roadLinksElement == null)
                return;
            foreach (XElement roadLinkElement in roadLinksElement.Elements("RoadLink"))
            {
                string type = roadLinkElement.Attribute("type")?.Value;
                int[] zoneIDs = ParseIntArray(roadLinkElement.Attribute("zoneID").Value);
                int roadNum = int.Parse(roadLinkElement.Attribute("roadNum").Value);
                switch (type)
                {
                    case "near":
                        foreach (int i in zoneIDs)
                        {
                            if (roadLinkElement.Attribute("num") == null)
                            {
                                foreach (ZoneVar dv in zones[i].zoneVars)
                                {
                                    dv.Road_link.Add(roadNum);
                                }
                            }
                            else
                            {
                                int[] subIDs = ParseIntArray(roadLinkElement.Attribute("num").Value);
                                foreach (int subID in subIDs)
                                {
                                    zones[i][subID].Road_link.Add(roadNum);
                                }
                            }
                        }
                        break;

                    case "align":
                        double tolerance = double.Parse(roadLinkElement.Attribute("tolerance")?.Value) / Unit;
                        foreach (int i in zoneIDs)
                        {
                            if (roadLinkElement.Attribute("num") == null)
                            {
                                foreach (ZoneVar dv in zones[i].zoneVars)
                                {
                                    dv.Road_align.Add(roadNum);
                                    dv.Align_tolerance = tolerance;
                                }
                            }
                            else
                            {
                                int[] subIDs = ParseIntArray(roadLinkElement.Attribute("num").Value);
                                foreach (int subID in subIDs)
                                {
                                    zones[i][subID].Road_align.Add(roadNum);
                                    zones[i][subID].Align_tolerance = tolerance;
                                }
                            }
                        }

                        break;
                    case "oneSide":
                        int side = int.Parse(roadLinkElement.Attribute("side").Value);
                        foreach (int i in zoneIDs)
                        {
                            if (roadLinkElement.Attribute("num") == null)
                            {
                                foreach (ZoneVar dv in zones[i].zoneVars)
                                {
                                    dv.RoadSide.Add(new int[] { roadNum, side });
                                }
                            }
                            else
                            {
                                int[] subIDs = ParseIntArray(roadLinkElement.Attribute("num").Value);
                                foreach (int subID in subIDs)
                                {
                                    zones[i][subID].RoadSide.Add(new int[] { roadNum, side });
                                }
                            }
                        }
                        break;
                }
            }
        }

        private void ParsePointLinks(XElement pointLinksElement, List<IZone> zones)
        {
            if (pointLinksElement == null)
                return;

            foreach (XElement pointLinkElement in pointLinksElement.Elements("PointLink"))
            {
                int[] zoneIDs = ParseIntArray(pointLinkElement.Attribute("zoneID").Value);
                int poiNum = int.Parse(pointLinkElement.Attribute("poiNum").Value);

                foreach (int i in zoneIDs)
                {
                    if (pointLinkElement.Attribute("num") == null)
                    {
                        foreach (ZoneVar dv in zones[i].ZoneVars)
                        {
                            dv.SingleP_link.Add(poiNum);
                        }
                    }
                    else
                    {
                        int[] subIDs = ParseIntArray(pointLinkElement.Attribute("num").Value);
                        foreach (int subID in subIDs)
                        {
                            zones[i][subID].SingleP_link.Add(poiNum);
                        }
                    }
                }
            }

        }
        #endregion

        #region utils
        private int[] ParseIntArray(string arrayStr)
        {
            // Check for null or empty string
            if (string.IsNullOrEmpty(arrayStr))
            {
                return new int[0];
            }

            // Remove curly braces and split by comma
            string[] parts = arrayStr.Trim('{', '}').Split(',');

            // Parse each part into an integer
            return parts.Select(part => int.Parse(part.Trim())).ToArray();
        }
        private float[] ParseFloatArray(string arrayStr)
        {
            // Check for null or empty string
            if (string.IsNullOrEmpty(arrayStr))
            {
                return new float[0];
            }

            // Remove curly braces and split by comma
            string[] parts = arrayStr.Trim('{', '}').Split(',');

            // Parse each part into an integer
            return parts.Select(part => float.Parse(part.Trim())).ToArray();


        }
        private double[] ParseDoubleArray(string arrayStr)
        {
            // Check for null or empty string
            if (string.IsNullOrEmpty(arrayStr))
            {
                return new double[0];
            }

            // Remove curly braces and split by comma
            string[] parts = arrayStr.Trim('{', '}').Split(',');

            // Parse each part into an integer
            return parts.Select(part => double.Parse(part.Trim())).ToArray();
        }
        #endregion
    }
}
