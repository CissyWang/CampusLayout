using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;
using Flowing;
using IndexCalculate;
using System.IO;

namespace InitialArrange
{
    public enum InOrOut
    {
        None=0,
        Inside = 1,
        Outside = 2
    }
    public class Calculator:GRB_Calculator
    {

        DistrictVar core;//中心区变量
        Domain[] core_domain;
        List<int> insideDistricts = new List<int>();
        bool insideOnly;
        bool insideAlign;
        List<int> outsideDistricts=new List<int>();
        bool outsideAlign=false;
        double coreK;
        double core_width;
        double core_lenToWidth=3;

        Line[] axis;//输入轴线
        int[][] AxisDistricts;//实轴分区
        int[][] AxisAsideDistricts;//虚轴分区

        double[] axis_width;
        double[] axis_buffer;
         bool[]  axis_center;


        List<DistrictVar> groups;//组团变量
        int groupCount;
        List<int[]> groupDistricts = new List<int[]>();
        List<double> groupK = new List<double>();
        List<Domain[]> group_domain;
        double g_lenToWidth=3;

        //网格
        LinearVar[] gridVars;
        List<int> gridIndex=new List<int>();
        List<Domain[]> grid_range = new List<Domain[]>();
        List<float> grid_width = new List<float>();
        List<int> directions= new List<int>();
        int gridLineCount;
        List<List<int>> districtsLorB = new List<List<int>>();
        List<List<int>> districtsRorT = new List<List<int>>();

        public Calculator(int grid, string siteCsv, Campus campus,string export) : base(grid, siteCsv, campus,export)
        {

        }
        public Calculator(int grid, string siteCsv,  string export) : base(grid, siteCsv,  export)
        {

        }
        ///分区信息输出Location表格
        public void ResponseExportCSV(string fileName)
        {
            if (fileName.Length <= 0)
            {
                return;
            }
            FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
            string dataHead;
            ///写入分区数据
            dataHead = "Districts ";
            for (int n = 0; n < resultCount; n++)
            {
                dataHead += "," + n.ToString();
            }
            sw.WriteLine(dataHead);
            foreach (DistrictVar dv in districtVars)
            {
                dv.WriteResults(sw, resultCount);
            }

            ///写入core/group
            sw.WriteLine("Rectangle");
            if (core != null)
            {
                core.WriteStructureResults(sw, resultCount);
            }
            if (groups != null)
            {
                foreach (DistrictVar g in groups)
                {
                    g.WriteStructureResults(sw, resultCount);
                }
            }

            ///写入RoadVar
            sw.WriteLine("Roads");
            if (gridVars != null)
            {
                foreach(LinearVar lv in gridVars)
                {
                    lv.WriteResults(sw, resultCount);
                }
            }
            ///写入轴线
            sw.WriteLine("Axis");
            string dataStr;
            if (axis != null)
            {
                for(int i=0;i<axis.Length;i++)
                {
                    try { 
                        dataStr = axis_width[i].ToString();
                    }
                    catch
                    {
                        dataStr = axis_buffer[i].ToString();
                    }
                    dataStr += $",({axis[i].X1}; {axis[i].Y1} ; {axis[i].X2} ; {axis[i].Y2})";
                    sw.WriteLine(dataStr);
                }
            }
            sw.Close();
            fs.Close();


        }

        override
        protected GRBQuadExpr CoreObj(GRBModel model)
        {
            GRBQuadExpr expr = new GRBQuadExpr();

            //中心区范围
            if (core_domain != null)
                core = new DistrictVar(site, core_domain, isInteger, core_lenToWidth);
            else
                core = new DistrictVar(site, isInteger, core_lenToWidth);
            core.SetVar(model);
            core.SiteConstr(model);
            for(int a = 0; a < districts.Count; a++) {
                foreach(DistrictVar dv in districts[a].districtVars)
                {
                    var b = outsideDistricts != null && outsideDistricts.Contains(a);
                    
                    //必须在内
                    if (insideDistricts != null && insideDistricts.Contains(a))
                    {
                        dv.inOrOutOthers(model, core, core_width, InOrOut.Inside, insideAlign);
                    }
                    //必须在外
                    else if (insideOnly&&!b)
                    {
                        dv.inOrOutOthers(model, core, core_width, InOrOut.Outside, false);
                    }else if(b)
                        dv.inOrOutOthers(model, core, core_width, InOrOut.Outside,outsideAlign); 
                    else
                    {
                        dv.inOrOutOthers(model, core, core_width, InOrOut.None, false);

                    }
                }
            }

            //目标
            expr.Add((core.dx * core.dy) * (100 * coreK / site.Area()));
            return expr;
        }

        override
         protected void AxisObj(GRBModel model)
        {
            if (axis == null)
                return;
            for (int i = 0; i < axis.Length; i++)
            {
                if (AxisAsideDistricts[i] != null)
                {
                    
                    foreach (int jj in AxisAsideDistricts[i])
                    {
                        foreach (DistrictVar dv in districts[jj].districtVars)
                        {
                            dv.LineAlign(model, axis[i], axis_width[i]);
                        }
                    }
                }
                if (AxisDistricts[i] != null)
                {

                    foreach (int j in AxisDistricts[i])
                    {
                        foreach (DistrictVar dv in districts[j].districtVars)
                        {
                            dv.LineAlign(model, axis[i], axis_buffer[i],axis_center[i]);
                        }
                    }
                }
            }
        }

        override
        protected GRBLinExpr GroupObj(GRBModel model)
        {
            GRBLinExpr expr = new GRBLinExpr();
            groups = new List<DistrictVar>();

            for(int i = 0; i < groupCount; i++)
            {
                DistrictVar g;
                //创建组团变量
                if (group_domain != null)
                    g= new DistrictVar(site, core_domain, isInteger, g_lenToWidth);
                else
                    g = new DistrictVar(site, isInteger, g_lenToWidth);

                g.SetVar(model);
                g.SiteConstr(model);
                groups.Add(g);

                if(groupDistricts[i].Length <= 0)
                {
                    Console.WriteLine($"组团{i}无效");
                    continue;
                }

                //限制在组团内
                for (int j = 0; j < districts.Count; j++)
                {
                    if (groupDistricts[i].Contains(j))
                    {
                        foreach (DistrictVar dv in districts[j].districtVars)
                        {
                            dv.inOrOutOthers(model, groups[i], 0, InOrOut.Inside, false);
                        }
                    }
                    //必须在外
                    else
                    {
                        foreach (DistrictVar dv in districts[j].districtVars)
                        {
                            dv.inOrOutOthers(model, groups[i], 0, InOrOut.Outside, outsideAlign);
                        }
                    }
                }

            //组团最小化
            expr.Add((groups[i].dx + groups[i].dy) * (100 * groupK[i] / (site.W+site.H)));
            }

            //组团互相不重叠
            for(int i = 0; i < groupCount - 1; i++)
            {
                groups[i].ConstrOverlap(model, groups, 0.5);
            }
            return expr;
        }

        override
        protected void GridObj(GRBModel model)
        {
            gridVars = new LinearVar[gridLineCount];
            for(int i=0;i< gridLineCount; i++)
            {
                gridVars[i]= new LinearVar(model, gridIndex[i],site,directions[i],grid_range[i],grid_width[i]);

                try {
                    gridVars[i].districtsLorB = districtsLorB[i];
                    
                } catch { }

                try
                {
                    gridVars[i].districtsRorT = districtsRorT[i];
                }
                catch { }
                gridVars[i].LineConstr(districtVars);
            }
        }

        override
        protected void setLoc(GRBModel model)
        {
            for (int n = 0; n < resultCount; n++)
            {
                model.Set(GRB.IntParam.SolutionNumber, n);
                Console.WriteLine("Result:" + n);
                foreach (DistrictVar dv in districtVars)
                {
                    dv.SetResult(model, n);
                   
                }
                if (core != null)
                {
                    core.SetResult(model, n);
                }
                if (groups!= null)
                {
                    foreach (DistrictVar dv in groups)
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
                foreach (DistrictVar dv in districtVars)
                {
                    areaResult[n] += dv.rectResults[n].Area * grid * grid;
                }
            }
        }

        public void Core_domain(int[] x,int[] y,int[]dx,int[]dy) {
            core_domain = new Domain[4];
            core_domain[0] = new Domain(x[0], x[1]);
            core_domain[1] = new Domain(y[0], y[1]);
            core_domain[2] = new Domain(dx[0], dx[1]);
            core_domain[3] = new Domain(dy[0], dy[1]);
        }
        public void Core_domain(int[] x, int[] y)
        {
            core_domain = new Domain[2];
            core_domain[0] = new Domain(x[0], x[1]);
            core_domain[1] = new Domain(y[0], y[1]);
        }

        public void Core_Outside(string districtIndex, bool align)
        {
            for (int i = 0; i < districts.Count; i++)
            {
                if (districtIndex.Contains("all"))
                {
                    outsideDistricts.Add(i);
                }
                else
                {
                    try
                    {
                        var indexes = districtIndex.Split(',');
                        if (indexes.Contains(i.ToString()))
                            outsideDistricts.Add(i);
                    }
                    catch
                    {
                        Console.WriteLine("CoreOutside选取失败");
                    }

                }
            }
            outsideAlign = align;
        }

        public void Core_Inside(string districtIndex,bool only,bool align)
        {
            for (int i = 0; i <districts.Count; i++)
            {
                if (districtIndex.Contains("all"))
                {
                    insideDistricts.Add(i);
                }
                else 
                {
                    try
                    {
                        var indexes = districtIndex.Split(',');
                        if (indexes.Contains(i.ToString()))
                            insideDistricts.Add(i);
                    }
                    catch
                    {
                        Console.WriteLine("CoreInside选取失败");
                    }
                }
            }
            insideOnly = only;
            insideAlign = align;
        }
        public void CoreK(double k)
        {
            coreK = k;
        }
        public DistrictVar Core { get => core; set => core = value; }
        public void Core_width (double width)
        {
            core_width = width;
        }
        public Line[] Axis { get => axis;}
        public void AddAxis(Line[] axis)
        {
            this.axis = axis;
            int count = axis.Length;
            AxisAsideDistricts = new int[count][];
            AxisDistricts = new int[count][];
            axis_buffer = new double[count];
            axis_center = new bool[count];
            axis_width = new double[count];

        }
        public void RealAxis(int axisNum,int[] districtNum,double width)//紧邻的分区
        {
            AxisAsideDistricts[axisNum]= districtNum;
            axis_width[axisNum]=width;
        }
        public void VirtualAxis(int axisNum, int[] distrctNum,double buffer,bool center)//在内的分区
        {
            AxisDistricts[axisNum]= distrctNum;
            axis_buffer[axisNum]= buffer;
            axis_center[axisNum]= center;
        }


        //增加组团
        public void SetGroup(int index,int[] insideDistricts,double minK)
        {
            groupCount ++;
            groupDistricts.Add(insideDistricts);
            groupK.Add(minK);
        }
        public void SetGroup(int index, int[] insideDistricts, double minK,double lenToWidth)
        {
            groupCount++;
            groupDistricts.Add(insideDistricts);
            groupK.Add(minK);
            g_lenToWidth = lenToWidth;
        }
        public List<DistrictVar> Groups { get => groups; }

        //增加网格
        public void AddGrid(Domain[] range,float gridWidth,int direction)
        {
            gridIndex.Add(gridLineCount);
            grid_range.Add(range);
            grid_width.Add(gridWidth);
            directions.Add(direction);
            gridLineCount++;
        }

        public void AddGrid(Domain[] range, float gridWidth, int direction,int[] districts,int side)
        {
            grid_range.Add(range);
            grid_width.Add(gridWidth);
            directions.Add(direction);
            gridIndex.Add(gridLineCount);

            List<int> op = new List<int>();
            List<int> ad = new List<int>();
            for (int i = 0; i < this.districts.Count; i++)
            {
                if (!districts.Contains(i))
                {
                    op.Add(i);
                }
                else { ad.Add(i); }
            }
            if (side == 0)
            {
                districtsLorB.Add(ad);
                districtsRorT.Add(op);
            }
            else if (side == 1)
            {
                districtsLorB.Add(op);
                districtsRorT.Add(ad);
            }
            gridLineCount++;
        }

        // 呈现网格
        public void Grids(int result,IApp app)
        {
            if (gridLineCount > 0 )
            {
                
                for (int i = 0; i < gridLineCount; i++)
                {
                    app.StrokeWeight(gridVars[i].width*grid);
                    var r= gridVars[i].ResultLine[result];
                    app.Line(grid * r.X1, grid * r.Y1, 0, grid * r.X2, grid * r.Y2, 0);
                }
            }

        }


    }
}
