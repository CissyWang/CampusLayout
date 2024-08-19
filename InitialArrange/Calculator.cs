///和GRB_Calculator（基类）合并了
///用于运行Gorubi， 包括从文件中读取分区信息并创建变量，从文件中读取场地信息
///通过XmlParser读取布局要求，运行优化，将结果导出。


using CampusClass;
using Gurobi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace InitialArrange
{
    public enum InOrOut
    {
        None = 0,
        Inside = 1,
        Outside = 2
    }
    public class Calculator
    {
        internal int unit;
        internal int resultCount = 5;
        internal int poolSearchMode = 0;
        //int districtCount;
        internal int dvCount;//计算分区数量
        internal double time = 50.0;

        ///设定场地
        internal Site site; //场地信息
        internal string fileName;//输出文件

        #region 设定各个分区
        internal int isInteger;//长宽变量是整数
        internal double spacing;//分区间间距

        internal List<IZone> zones = new List<IZone>();
        internal List<ZoneVar> zoneVars = new List<ZoneVar>();//所有分区变量列表
        List<int[]> zoneLink = new List<int[]>();//两个分区拓扑关系
        List<int> zoneDist = new List<int>();//分区远离

        internal double[] sportInfo;
        internal double[] weights;
        #endregion

        double totalArea;//校舍总用地面积
        bool totalAreaLimit = false;
        double layoutDensity;//场地占用率
        protected float[] areaResult;//分区结果总面积

        string mode = "";
        internal IGroup core;//中心区
        ZoneVar coreVar;//中心区变量
        internal List<Axis> axes;//输入轴线，常量
        internal List<IGroup> groups;//组团
        List<ZoneVar> groupVars;//组团变量       
        internal List<LinearVar> gridVars; //网格 变量

        protected XmlParser parser;

        public Calculator(string xmlFilePath)
        {
            XmlParser parser = new XmlParser(xmlFilePath);
            parser.BasicSettings(this);

            //如果有json，利用json
            string jsonPath = parser.Filepaths[0].Replace(".csv", ".json");
            string json = "";
            try
            {
                json = File.ReadAllText(jsonPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("读取JSON时发生错误：" + ex.Message);
            }
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new BuildingListConverter() }
            };
            List<Zone> zonesOri = JsonConvert.DeserializeObject<List<Zone>>(json, settings);
            //List<Zone> zonesOri = JsonConvert.DeserializeObject<List<Zone>>(json);
            foreach (Zone d in zonesOri)
            {
                IZone newD = new IZone(d, unit);
                zones.Add(newD);
            }



            //从csv读取分区并新建或只是读取数量
            ReadZones(parser.Filepaths[0]);
            SetZoneVar();
            dvCount = zoneVars.Count; //读取分区数量
            Console.WriteLine("分区变量：" + dvCount + "项");

            //分区变量的通用控制
            parser.ShapeSettings(this);
            parser.TopologySettings(this);
            parser.StructureSettings(this);
        }
        ///***构造函数***
        public Calculator(string xmlFilePath, Campus campus)
        {
            XmlParser parser = new XmlParser(xmlFilePath);
            parser.BasicSettings(this);

            foreach (Zone d in campus.Zones)
            {
                IZone newD = new IZone(d, unit);
                zones.Add(newD);
            }

            //读取分区数量
            ReadZones(parser.Filepaths[0]);
            SetZoneVar();
            dvCount = zoneVars.Count;
            Console.WriteLine("分区变量：" + dvCount + "项");

            //分区变量的通用控制
            parser.ShapeSettings(this);
            parser.TopologySettings(this);
            parser.StructureSettings(this);
        }

        ///*****运行Gurobi*****
        public void runGRB(string mode)
        {
            try
            {
                GRBEnv env = new GRBEnv(true);
                var s = fileName.Replace("csv", "log");
                env.Set("LogFile", s);

                env.Start();

                GRBModel model = new GRBModel(env);
                SetBasicVar(model);

                //目标
                GRBQuadExpr objExpr = new GRBQuadExpr();
                objExpr.Add(basicObj(model, weights[0], weights[1])); //权重已转移至此
                if (mode.Contains("Core"))
                {
                    Console.WriteLine("中心区模式");
                    objExpr.Add(CoreObj(model));
                }
                if (mode.Contains("Axes"))
                {
                    Console.WriteLine("轴线模式");
                    AxisObj(model);
                }
                if (mode.Contains("Groups"))
                {
                    Console.WriteLine("组团模式");
                    objExpr.Add(GroupObj(model));
                }
                if (mode.Contains("Grids"))
                {
                    Console.WriteLine("网格模式");
                    GridObj(model);
                }
                model.SetObjective(objExpr, GRB.MINIMIZE);

                //优化设置
                model.Set(GRB.IntParam.NonConvex, 2);
                model.Set(GRB.IntParam.PoolSearchMode, poolSearchMode);//default:
                //model.Set(GRB.DoubleParam.PoolGap, 3);
                model.Set(GRB.IntParam.PoolSolutions, resultCount);
                model.Set(GRB.DoubleParam.TimeLimit, time);

                //运行
                model.Optimize();

                //导出
                setLoc(model);
                Console.WriteLine("Obj: " + model.ObjVal);

                // 关闭模型
                model.Dispose();
                env.Dispose();
            }
            catch (GRBException e)
            {
                Console.WriteLine("Error code: " + e.ErrorCode + ". " + e.Message);
            }
        }
        ///分区信息输出Location表格
        public void ResponseExportCSV()
        {

            if (fileName.Length <= 0)
            {
                return;
            }
            FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
            string dataHead;
            ///写入分区数据
            dataHead = "Zones ";
            for (int n = 0; n < resultCount; n++)
            {
                dataHead += "," + n.ToString();
            }
            sw.WriteLine(dataHead);
            foreach (ZoneVar dv in zoneVars)
            {
                dv.WriteResults(sw, resultCount);
            }

            ///写入core/group
            sw.WriteLine("Rectangle");
            if (core != null)
            {
                coreVar.WriteStructureResults(sw, resultCount);
            }
            if (groups != null)
            {
                foreach (ZoneVar g in GroupVars)
                {
                    g.WriteStructureResults(sw, resultCount);
                }
            }

            ///写入RoadVar
            sw.WriteLine("Roads");
            if (gridVars != null)
            {
                foreach (LinearVar lv in gridVars)
                {
                    lv.WriteResults(sw, resultCount);
                }
            }
            ///写入轴线
            sw.WriteLine("Axis");
            string dataStr;
            if (axes != null)
            {
                for (int i = 0; i < axes.Count; i++)
                {
                    try
                    {
                        dataStr = axes[i].width.ToString();
                    }
                    catch
                    {
                        dataStr = axes[i].buffer.ToString();
                    }
                    dataStr += $",({axes[i].X1}; {axes[i].Y1} ; {axes[i].X2} ; {axes[i].Y2})";
                    sw.WriteLine(dataStr);
                }
            }
            sw.Close();
            fs.Close();


        }

        ///****读取分区信息***
        // 连接上一步的
        private void ReadZones(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs, Encoding.UTF8);
            string str = "";
            bool start = false;
            int index = 0;
            while ((str = sr.ReadLine()) != null)
            {
                if (str.Contains("校舍用地面积"))
                {
                    string result = System.Text.RegularExpressions.Regex.Replace(str, @"[^0-9]+", "");
                    totalArea += int.Parse(result);
                    continue;
                }

                if (str.Contains("分区名"))
                {
                    start = true;
                    continue;
                }
                if (start)
                {
                    string[] strs = str.Split(',');
                    double d_siteArea = Convert.ToDouble(strs[2]);
                    int d_count = 1;
                    if (strs.Length > 6 && strs[6] != null)
                    {
                        d_count = int.Parse(strs[6]);
                    }

                    if (zones.Count > index)
                    {
                        zones[index].Site_area = d_siteArea;//
                        zones[index].Count = d_count;
                    }
                    else//不连接上一步时
                    {
                        var zone = new IZone(index, strs[0], d_siteArea, Convert.ToDouble(strs[3]), d_count, unit);
                        dvCount += d_count;//总的分区变量数量
                        zones.Add(zone);
                    }
                    index++;
                }
            }

        }

        private void SetZoneVar()
        {
            foreach (IZone d in zones)
            {
                Console.WriteLine("\r\n");
                Console.WriteLine(d.name + d.Count);
                d.ZoneVars = new ZoneVar[d.Count];
                for (int i = 0; i < d.Count; i++)
                {
                    d.zoneVars[i] = new ZoneVar(d, site, isInteger);
                    zoneVars.Add(d.zoneVars[i]);
                }
            }
        }

        #region 基础变量、限制、目标
        protected void SetBasicVar(GRBModel model)
        {
            //变量初始设置
            foreach (ZoneVar dv in zoneVars)
            {
                dv.SetVar(model, spacing);
                dv.SiteConstr(model, spacing);
                //model.AddConstr(dv.dx >= lengthMin, "");
                //model.AddConstr(dv.dy >= lengthMin, "");
            }

            foreach (IZone d in zones)
            {
                //体育区尺寸控制
                if (d.Building_area == 0)
                {
                    model.AddConstr(d.zoneVars[0].dx >= sportInfo[0] / unit, " ");
                    model.AddConstr(d.zoneVars[0].dy >= sportInfo[1] / unit, " ");
                }
            }

            AreaConstr(model);

            for (int i = 0; i < dvCount - 1; i++)
            {
                zoneVars[i].ConstrOverlap(model, zoneVars, spacing);
            }
        }

        private void AreaConstr(GRBModel model)
        {
            GRBQuadExpr expr = new GRBQuadExpr();
            GRBQuadExpr expr1 = new GRBQuadExpr();

            ///分区面积限制
            foreach (IZone d in zones)
            {
                //表格读取的面积要求 
                foreach (ZoneVar dv in d.zoneVars)
                {
                    expr.Add(dv.dx * dv.dy);//求功能区的总面积
                }
                //设置单个范围or设置总浮动
                if (d.area_lim == null)
                {
                    model.AddQConstr(expr >= d.S, "dx*dy>area");
                }
                else
                {
                    model.AddQConstr(expr <= d.area_lim.max, "area<area_max");
                    model.AddQConstr(expr >= d.area_lim.min, "area>area_min");
                }

                //清空
                expr.Clear();
            }

            ///总体面积限制
            foreach (ZoneVar dv in zoneVars)
            {
                expr1.AddTerm(1, dv.dx, dv.dy);
                if (dv.Zone.building_area > 0)
                    expr.AddTerm(1, dv.dx, dv.dy);
            }
            if (totalAreaLimit)
                model.AddQConstr(expr >= totalArea / unit / unit, " "); //校舍总用地面积达到要求

            if (layoutDensity > 0)
                model.AddQConstr(expr1 >= layoutDensity * site.Area(), " S_totall> r%*site.area");
        }

        protected GRBQuadExpr basicObj(GRBModel model, double areaK, double distK)
        {
            var basicObj = new GRBQuadExpr();
            //*面积
            if (areaK > 0)
            {
                GRBQuadExpr expr = new GRBQuadExpr();
                foreach (ZoneVar dv in zoneVars)
                {
                    ///面积占用率（0—100）
                    if (dv.Zone.building_area > 0)
                        expr.AddTerm(1, dv.dx, dv.dy);
                }
                double k = areaK * 100;
                double total = site.Area();
                Console.WriteLine("可利用面积=" + total);
                basicObj.Add((k / total) * (total - expr));
                expr.Clear();
            }
            //*距离
            if (distK == 0)
            { return basicObj; }

            //到场地点线距离
            foreach (ZoneVar dv in zoneVars)
            {
                ///距离指标（0—100）
                if (dv.SingleP_link != null && dv.SingleP_link.Count > 0)
                    dv.SetEntranceObj(basicObj, distK * 100 / dv.SingleP_link.Count);
                if (dv.Road_link != null && dv.Road_link.Count > 0)
                    dv.SetRoadObj(basicObj, 1);
                if (dv.Road_align != null && dv.Road_align.Count > 0)
                    dv.SetRoadConstr(model);
            }
            //分区拓扑
            if (zoneLink != null)
            {
                foreach (int[] ds in zoneLink)
                {
                    var district1 = zones[ds[0]];
                    var district2 = zones[ds[1]];
                    int c = district2.Count;
                    if (district1.Count < district2.Count)
                        c = district1.Count;
                    for (int i = 0; i < c; i++)
                    {
                        district1[i].ZoneLinkObj(basicObj, district2[i]);
                    }

                }
            }
            if (zoneDist != null)
            {
                foreach (int i in zoneDist)
                {
                    IZone d = zones[i];
                    int c = d.Count;
                    for (int j = 0; j < c - 1; j++)
                    {

                        d.zoneVars[j].ZoneDistObj(basicObj, d.zoneVars[j + 1]);
                    }
                }
            }
            return basicObj;
        }

        protected void setLoc(GRBModel model)
        {
            for (int n = 0; n < resultCount; n++)
            {
                model.Set(GRB.IntParam.SolutionNumber, n);
                Console.WriteLine("Result:" + n);
                foreach (ZoneVar dv in zoneVars)
                {
                    dv.SetResult(model, n);

                }
                if (core != null)
                {
                    coreVar.SetResult(model, n);
                }
                if (groups != null)
                {
                    foreach (ZoneVar dv in GroupVars)
                    {
                        dv.SetResult(model, n);
                    }
                }
                //网格道路
                if (gridVars != null)
                {
                    foreach (LinearVar lv in gridVars)
                    {
                        lv.setResults(model);
                    }
                }
            }

            //计算布局总面积
            areaResult = new float[resultCount];
            for (int n = 0; n < resultCount; n++)
            {
                foreach (ZoneVar dv in zoneVars)
                {
                    areaResult[n] += dv.rectResults[n].Area * unit * unit;
                }
            }
        }
        #endregion

        # region set structure 

        protected GRBQuadExpr CoreObj(GRBModel model)
        {
            GRBQuadExpr expr = new GRBQuadExpr();
            //中心区范围
            if (core.domain != null)
                coreVar = new ZoneVar(site, core.domain, isInteger, core.lenToWidth); //在这里新建变量
            else
                coreVar = new ZoneVar(site, isInteger, core.lenToWidth);
            coreVar.SetVar(model, spacing);
            coreVar.SiteConstr(model, spacing);
            for (int a = 0; a < zones.Count; a++)
            {
                foreach (ZoneVar dv in zones[a].zoneVars)
                {
                    var b = core.outsideZones != null && core.outsideZones.Contains(a);
                    //必须在内
                    if (core.insideZones != null && core.insideZones.Contains(a))
                    {
                        dv.inOrOutOthers(model, coreVar, core.stroke / unit, InOrOut.Inside, core.insideAlign);
                    }
                    //必须在外
                    else if (core.insideOnly && !b)
                    {
                        dv.inOrOutOthers(model, coreVar, core.stroke / unit, InOrOut.Outside, false);
                    }
                    else if (b)
                        dv.inOrOutOthers(model, coreVar, core.stroke / unit, InOrOut.Outside, core.outsideAlign);
                    else
                    {
                        dv.inOrOutOthers(model, coreVar, core.stroke / unit, InOrOut.None, false);
                    }
                }
            }

            //目标
            expr.Add((coreVar.dx * coreVar.dy) * (100 * core.minimizeWeight / site.Area()));
            return expr;
        }

        protected void AxisObj(GRBModel model)
        {
            if (axes == null)
                return;
            for (int i = 0; i < axes.Count; i++)
            {
                if (axes[i].asideZones != null)
                {
                    foreach (int jj in axes[i].asideZones)
                    {
                        foreach (ZoneVar dv in zones[jj].zoneVars)
                        {
                            dv.LineAlign(model, axes[i], axes[i].width / unit);
                        }
                    }
                }
                if (axes[i].onZones != null)
                {

                    foreach (int j in axes[i].onZones)
                    {
                        foreach (ZoneVar dv in zones[j].zoneVars)
                        {
                            dv.LineAlign(model, axes[i], axes[i].buffer / unit, axes[i].isCenter);
                        }
                    }
                }
            }
        }

        protected GRBLinExpr GroupObj(GRBModel model)
        {

            GRBLinExpr expr = new GRBLinExpr();
            groupVars = new List<ZoneVar>();
            foreach (IGroup IG in groups)
            {
                if (IG.insideZones.Count <= 0)
                {
                    Console.WriteLine($"组团{IG.Index}无效"); //不包含内容的组团
                    continue;
                }

                ZoneVar g;
                g = new ZoneVar(site, isInteger, IG.lenToWidth);
                g.SetVar(model, spacing);
                g.SiteConstr(model, spacing);
                IG.zoneVar = g;
                GroupVars.Add(g);

                //限制在组团内
                for (int j = 0; j < zones.Count; j++)
                {
                    if (IG.insideZones.Contains(j))
                    {
                        foreach (ZoneVar dv in zones[j].zoneVars)
                        {
                            dv.inOrOutOthers(model, g, IG.stroke, InOrOut.Inside, false);
                        }
                    }
                    //必须在外
                    else
                    {
                        foreach (ZoneVar dv in zones[j].zoneVars)
                        {
                            dv.inOrOutOthers(model, g, IG.stroke, InOrOut.Outside, false);
                        }
                    }
                }

                //组团最小化
                expr.Add((g.dx + g.dy) * (100 * IG.minimizeWeight / (site.W + site.H)));
            }

            //组团互相不重叠
            for (int i = 0; i < groups.Count - 1; i++)
            {
                groups[i].zoneVar.ConstrOverlap(model, GroupVars, 0.5);
            }
            return expr;
        }

        protected void GridObj(GRBModel model)
        {
            for (int i = 0; i < gridVars.Count; i++)
            {
                gridVars[i].SetVar(model, site);
                gridVars[i].LineConstr(model, zoneVars, site);
            }
        }
        #endregion

        #region 封装
        public ZoneVar CoreVar { get => coreVar; set => coreVar = value; }
        public List<Axis> Axes { get => axes; }
        public List<IGroup> Groups { get => groups; }

        public string Mode { get => mode; set => mode = value; }
        public List<ZoneVar> GroupVars { get => groupVars; set => groupVars = value; }
        public IGroup Core { get => core; set => core = value; }
        public List<LinearVar> GridVars { get => gridVars; }

        public string SiteCsv { set => SiteCsv = value; }

        public int IsInteger { set => isInteger = value; }
        public List<IZone> Zones { get => zones; }
        public Site Site { get => site; }

        public float[] AreaResult { get => areaResult; }
        public int ResultCount { set => resultCount = value; get => resultCount; }
        public int PoolSearchMode { set => poolSearchMode = value; }

        public List<ZoneVar> ZoneVars { get => zoneVars; set => zoneVars = value; }
        public double Time { get => time; set => time = value; }
        public int Unit { get => unit; }
        public double TotalArea { get => totalArea; set => totalArea = value; }
        public bool TotalAreaLimit { get => totalAreaLimit; set => totalAreaLimit = value; }
        public double LayoutDensity { get => layoutDensity; set => layoutDensity = value; }
        public double Spacing { get => spacing; set => spacing = value; }
        public double[] SportInfo { get => sportInfo; set => sportInfo = value; }
        public List<int[]> ZoneLink { get => zoneLink; set => zoneLink = value; }
        public List<int> ZoneDist { get => zoneDist; set => zoneDist = value; }

        #endregion
    }
}
