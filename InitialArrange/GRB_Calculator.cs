using System;
using System.Collections.Generic;
using System.IO;
using Gurobi;
using System.Text;
using IndexCalculate;
using System.Linq;


namespace InitialArrange
{

    public class GRB_Calculator
    {
        protected int unit;
        protected int resultCount= 5;
        protected int poolSearchMode=0;
        //int districtCount;
        protected int dvCount;//计算分区数量
        protected double time = 50.0;
        protected string path;
        ///设定场地
        protected Site site; //场地信息
        protected string fileName;//输出文件

        #region 设定各个分区
        protected int isInteger;//长宽变量是整数
        protected double spacing;//分区间间距

        internal List<IZone> zones = new List<IZone>();

        protected List<ZoneVar> zoneVars = new List<ZoneVar>();//所有分区变量列表
        List<int[]> zoneLink = new List<int[]>();//两个分区拓扑关系
        List<int> zoneDist = new List<int>();//分区远离

        protected double[] sportInfo=new double[]{100, 200 };

        protected double[] weights;
        #endregion

        double totalArea;//校舍总用地面积
        bool totalAreaLimit=false;
        double layoutDensity;//场地占用率

        protected float[] areaResult;//分区结果总面积


        ///***构造函数***
        public GRB_Calculator(int unit, string siteCsv, Campus campus,string export,string output)
        {
            ///设置场地
            this.unit = unit;
            site = new Site(siteCsv);
            this.path = export;
            ///读取分区并新建
            foreach(Zone d in campus.Zones)
            {
                IZone newD = new IZone(d, unit);
                zones.Add(newD);
            }

            //读取分区数量
            ReadZones();
            dvCount = zoneVars.Count;
            Console.WriteLine("分区变量："+dvCount+"项");
            
        }

        public GRB_Calculator(int unit, string siteCsv, string export,string output)
        {
            ///设置场地
            this.unit = unit;
            site = new Site(siteCsv);
            this.path = export;
            ///读取分区并新建

            //读取分区数量
            ReadZones1();
            dvCount = zoneVars.Count;
            Console.WriteLine("分区变量：" + dvCount + "项");
            this.fileName = output;
        }

        public GRB_Calculator(string xmlFilePath)
        {

        }

        ///****读取分区信息***
        /// 连接上一步的
        private void ReadZones()
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

                    if (strs[2] != null)
                    {
                        double d_siteArea = Convert.ToDouble(strs[2]);//
                        zones[index].Site_area = d_siteArea;//
                    }

                    int d_count=1;
                    if (strs.Length > 6 && strs[6] != null)
                    {
                        d_count = int.Parse(strs[6]);
                    }
                    
                    zones[index].Count = d_count;
                   
                    index++;
                }
            }
            //新建分区变量
            Console.WriteLine("");
            foreach (IZone d in zones)
            {
                Console.WriteLine(d.name + d.Count);
                d.ZoneVars = new ZoneVar[d.Count];
                for (int i = 0; i < d.Count; i++)
                {
                    d.zoneVars[i] = new ZoneVar(d, site, isInteger);
                    zoneVars.Add(d.zoneVars[i]);
                }
            }

        }

        //不连接上一步时
        protected void ReadZones1()
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
                    // double plotRatio = Convert.ToDouble(strs[4]);
                    // double density = Convert.ToDouble(strs[5]);
                    int d_count = 1;
                    if (strs.Length > 6 && strs[6] != null)
                    {
                        d_count = int.Parse(strs[6]);
                    }
                    var zone = new IZone(index,strs[0], d_siteArea, Convert.ToDouble(strs[3]),  d_count, unit);

                    //新建分区同时新建分区变量
                    dvCount += d_count;//总的分区变量数量

                    //读取分区连接点的信息
                    //if (strs.Length >= 10)
                    //{
                    //    if (strs[7].Split('{', '}').Length > 1)
                    //        zone.sigleP_link = ReadLinks(strs[7].Split('{', '}'));
                    //    if (strs[8].Split('{', '}').Length > 1)
                    //        zone.road_link = ReadLinks(strs[8].Split('{', '}'));
                    //    if (strs[9].Split('{', '}').Length > 1)
                    //        zone.road_align = ReadLinks(strs[9].Split('{', '}'));
                    //}
                    zones.Add(zone);
                    index++;
                }
            }
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

        ///*****运行Gurobi*****
        public void runGRB(string mode,double areaK,double distK)
        {
            try
            {
                GRBEnv env = new GRBEnv(true);
                env.Set("LogFile", "mip1.log");
                env.Start();

                GRBModel model = new GRBModel(env);
                SetBasicVar(model);

                //目标
                GRBQuadExpr objExpr = new GRBQuadExpr();
                objExpr.Add(basicObj(model, areaK, distK));
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
                model.Set(GRB.StringParam.LogFile, fileName.Replace(".csv", ".log"));
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
        public void runGRB(string mode)
        {
            try
            {
                GRBEnv env = new GRBEnv(true);
                env.Set("LogFile", "mip1.log");
                env.Start();

                GRBModel model = new GRBModel(env);
                SetBasicVar(model);

                //目标
                GRBQuadExpr objExpr = new GRBQuadExpr();
                objExpr.Add(basicObj(model, weights[0],weights[1])); //权重已转移至此
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

        #region 四种模式虚拟方法
        protected virtual GRBQuadExpr CoreObj(GRBModel model)
        {
            GRBQuadExpr expr = new GRBQuadExpr();
            return expr;
        }
        protected virtual void AxisObj(GRBModel model)
        {}
        protected virtual GRBLinExpr GroupObj(GRBModel model)
        {
            GRBLinExpr expr = new GRBLinExpr();
            return expr;
        }

        protected virtual void GridObj(GRBModel model)
        {
        }
        #endregion

        #region 基础变量、限制、目标
        protected void SetBasicVar(GRBModel model)  
        {
            //变量初始设置
            foreach(ZoneVar dv in zoneVars)
            {
                dv.SetVar(model);
                dv.SiteConstr(model);
                //model.AddConstr(dv.dx >= lengthMin, "");
                //model.AddConstr(dv.dy >= lengthMin, "");
            }

            foreach (IZone d in zones)
            {
                //体育区尺寸控制
                if (d.Building_area == 0)
                {
                    model.AddConstr(d.zoneVars[0].dx >= sportInfo[0]/unit, " ");
                    model.AddConstr(d.zoneVars[0].dy >= sportInfo[1] / unit, " ");
                }
            }

            AreaConstr(model);

            for (int i = 0; i < dvCount - 1; i++)
            {
                zoneVars[i].ConstrOverlap(model, zoneVars, spacing);
            }
        }

        private void AreaConstr(GRBModel model) {
            GRBQuadExpr expr= new GRBQuadExpr();
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

        protected GRBQuadExpr basicObj(GRBModel model,double areaK,double distK)
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
            {return basicObj;  }

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
                foreach(int i in zoneDist)
                {
                    IZone d = zones[i];
                    int c = d.Count;
                    for (int j = 0; j< c-1; j++)
                    {
                        
                        d.zoneVars[j].ZoneDistObj(basicObj, d.zoneVars[j+1]);
                    }
                }
            }
            return basicObj;
        }

        protected virtual void setLoc(GRBModel model)
        {
        }
        #endregion

        #region 配置接口
        #region Topology
        public void PointLink(int index1,int index2,string entrances)
        {
            for (int i = 0; i < site.Entrances.Count; i++)
            {
                if (entrances.Contains(i.ToString()))
                {
                    zones[index1].zoneVars[index2].SingleP_link.Add(i);
                }
            }
        }
        public void PointLink(int index1, string entrances)
        {
            for (int i = 0; i < site.Entrances.Count; i++)
            {
                if (entrances.Contains(i.ToString()))
                {
                    foreach(ZoneVar dv in zones[index1].zoneVars) {
                        dv.SingleP_link.Add(i);
                    }
                }
            }
        }

        public void RoadLink(int index1, int index2, string roads)
        {
            for (int i = 0; i < site.Roads.Count; i++)
            {
                if (roads.Contains(i.ToString()))
                {
                    zones[index1].zoneVars[index2].Road_link.Add(i);
                }
            }
        }

        public void RoadLink(int index1, string roads)
        {
            for (int i = 0; i < site.Roads.Count; i++)
            {
                if (roads.Contains(i.ToString()))
                {
                    foreach (ZoneVar dv in zones[index1].zoneVars)
                    {
                        dv.Road_link.Add(i);
                    }
                }
            }
        }

        public void RoadAlign(int index1, int index2, string roads, double tolerance)
        {
            zones[index1].zoneVars[index2].Align_tolerance = tolerance;
            for (int i = 0; i < site.Roads.Count; i++)
            {
                if (roads.Contains(i.ToString()))
                {
                    zones[index1].zoneVars[index2].Road_align.Add(i);
                }
            }
        }
        public void RoadAlign(int index1,  string roads, double tolerance)
        {
            foreach(ZoneVar dv in zones[index1].zoneVars)
            {
                dv.Align_tolerance = tolerance;
            }
            
            for (int i = 0; i < site.Roads.Count; i++)
            {
                if (roads.Contains(i.ToString()))
                {
                    foreach (ZoneVar dv in zones[index1].zoneVars)
                    {
                        dv.Road_align.Add(i);
                    }
                }
            }
        }

        public void RoadSingleSide(string districtIndex, int road, int side)
        {
            try
            {
                var indexes = districtIndex.Split(',');
                for (int i = 0; i < zones.Count; i++)
                {
                    if (indexes.Contains(i.ToString()))
                    {
                        foreach (ZoneVar dv in zones[i].zoneVars)
                        {
                            dv.RoadSide.Add(new int[] { road, side });
                            //Console.WriteLine($"{road},{side}");
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("RoadSingleSide选取失败");
            }

        }

        public void SetZoneLink(int district1,int district2)
        {
            zoneLink.Add(new int[] { district1, district2 });
        }

        public void SetSpacing(double dist)
        {
            spacing = dist / unit;
        }

        public void SetZoneDist(int index)
        {
            zoneDist.Add(index);
        }

        #endregion

        #region 尺寸控制
        public void LenToWidth(double lenToWidth){
            foreach (ZoneVar dv in zoneVars)
            {
                dv.lenToWidth = lenToWidth;
            }
        }

        //总面积浮动
        public void AreaFloats(double scale1,double scale2)
        {
            foreach (IZone d in zones)
            {
                d.area_lim = new Domain(scale1 * d.S, scale2 * d.S);
            }
        }
        public void AreaFloats(double scale1)
        {
            foreach (IZone d in zones)
            {
                d.Site_area = scale1 * d.Site_area;
            }
        }

        //父分区面积浮动
        public void AreaFloats(int index, double deta1, double deta2)
        {
            zones[index].area_lim = new Domain( deta1 * zones[index].S, deta2* zones[index].S);
        }


        //子分区
        public void AreaSep(int index, double k)
        {
            if (k > 1 || k < 0)
            {
                return;
            }
            if (zones[index].Count > 1)
            {
                foreach(ZoneVar dv in zones[index].zoneVars)
                {
                    dv.Area_lim.min = k * zones[index].S / zones[index].Count;
                }
            }
        }
        public void AreaSep(double k)
        {
            if (k > 1 || k < 0)
            {
                return;
            }
            foreach (IZone d in zones)
            {
                if (d.Count > 1&&d.Building_area>0)
                {
                    foreach (ZoneVar dv in d.zoneVars)
                    {
                        dv.Area_lim.min = k* d.S/d.Count ;
                    }
                }
            }
        }
        public void LengthMin(double min)
        {
            if (min > 0)
            {
                foreach(IZone z in zones)
                {
                  z.Length_min = min / unit;
                }
            }
        }
        public void LengthMin(int index,double min)
        {
            if (min > 0)
            {
                zones[index].Length_min = min/unit;
            }
        }
        public void SportArea(double X,double Y)
        {
            sportInfo[0]=X;
            sportInfo[1] = Y;
        }

        //总面积控制
        public void LimitTotalArea()
        {
            totalAreaLimit = true;
        }
        public void LimitTotalArea(double area)
        {
            totalAreaLimit = true;
            totalArea = area;
        }

        //设置分区布局的密度
        public void SetLayoutDensity(double density)
        {
            if (density > 0 && density < 1)
            {
                this.layoutDensity = density;
            }
            else
            {
                Console.WriteLine(" density wrong");
            }
        }
        #endregion
        #endregion

        #region 封装
        public string SiteCsv { set => SiteCsv = value; }
        //public string ZonesCsv { set => districtsCsv = value; }
        public int IsInteger { set => isInteger = value; }
        public List<IZone> Zones { get => zones; }
        public Site Site { get => site; }

        public float[] AreaResult { get => areaResult; }
        public int ResultCount { set => resultCount = value;get =>resultCount; }
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