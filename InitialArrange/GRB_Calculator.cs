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
        protected int grid; 
        protected int resultCount =5;
        int poolSearchMode=0;
        //int districtCount;
        protected int dvCount;//�����������
        double time = 50.0;
        string path;
        ///�趨����
        protected Site site; //������Ϣ

        #region �趨��������
        protected int isInteger = 0;//�������������
        protected double spacing = 0.0;//��������

        internal List<IDistrict> districts = new List<IDistrict>();

        protected List<DistrictVar> districtVars = new List<DistrictVar>();//���з��������б�
        List<int[]> districtLink = new List<int[]>();//�����������˹�ϵ
        List<int> districtDist = new List<int>();//����Զ��

        double sportX=100;
        double sportY=200;
        double lengthMin=0;
        #endregion

        double totalArea;//У�����õ����
        bool totalAreaLimit=false;
        double layoutDensity;//����ռ����

        protected float[] areaResult;//������������



        ///***���캯��***

        public GRB_Calculator(int grid, string siteCsv, Campus campus,string export)
        {
            ///���ó���
            this.grid = grid;
            site = new Site(siteCsv);
            this.path = export;
            ///��ȡ�������½�
            foreach(District d in campus.Districts)
            {
                IDistrict newD = new IDistrict(d, grid);
                districts.Add(newD);
            }

            //��ȡ��������
            ReadDistricts();
            dvCount = districtVars.Count;
            Console.WriteLine("����������"+dvCount+"��");
            
        }

        public GRB_Calculator(int grid, string siteCsv, string export)
        {
            ///���ó���
            this.grid = grid;
            site = new Site(siteCsv);
            this.path = export;
            ///��ȡ�������½�

            //��ȡ��������
            ReadDistricts1();
            dvCount = districtVars.Count;
            Console.WriteLine("����������" + dvCount + "��");

        }

        ///****��ȡ������Ϣ***
        private void ReadDistricts()
        {
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs, Encoding.UTF8);
            string str = "";
            bool start = false;
            int index = 0;
            while ((str = sr.ReadLine()) != null)
            {
                if (str.Contains("У���õ����"))
                {
                    string result = System.Text.RegularExpressions.Regex.Replace(str, @"[^0-9]+", "");
                    totalArea += int.Parse(result);
                    continue;
                }

                if (str.Contains("������"))
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
                        districts[index].Site_area = d_siteArea;//
                    }

                    int d_count=1;
                    if (strs.Length > 6 && strs[6] != null)
                    {
                        d_count = int.Parse(strs[6]);
                    }
                    
                    districts[index].Count = d_count;
                   
                    index++;
                }
            }
            //�½���������
            Console.WriteLine("");
            foreach (IDistrict d in districts)
            {
                Console.WriteLine(d.name + d.Count);
                d.DistrictVars = new DistrictVar[d.Count];
                for (int i = 0; i < d.Count; i++)
                {
                    d.districtVars[i] = new DistrictVar(d, site, isInteger);
                    districtVars.Add(d.districtVars[i]);
                }
            }

        }
        private void ReadDistricts1()
        {
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs, Encoding.UTF8);
            string str = "";
            bool start = false;
            int index = 0;
            while ((str = sr.ReadLine()) != null)
            {
                if (str.Contains("У���õ����"))
                {
                    string result = System.Text.RegularExpressions.Regex.Replace(str, @"[^0-9]+", "");
                    totalArea += int.Parse(result);
                    continue;
                }

                if (str.Contains("������"))
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
                    var district = new IDistrict(index,strs[0], d_siteArea, Convert.ToDouble(strs[3]),  d_count, grid);

                    //�½�����ͬʱ�½���������
                    dvCount += d_count;//�ܵķ�����������

                    //��ȡ�������ӵ����Ϣ
                    //if (strs.Length >= 10)
                    //{
                    //    if (strs[7].Split('{', '}').Length > 1)
                    //        district.sigleP_link = ReadLinks(strs[7].Split('{', '}'));
                    //    if (strs[8].Split('{', '}').Length > 1)
                    //        district.road_link = ReadLinks(strs[8].Split('{', '}'));
                    //    if (strs[9].Split('{', '}').Length > 1)
                    //        district.road_align = ReadLinks(strs[9].Split('{', '}'));
                    //}
                    districts.Add(district);
                    index++;
                }
            }
            foreach (IDistrict d in districts)
            {
                Console.WriteLine("\r\n");
                Console.WriteLine(d.name + d.Count);
                d.DistrictVars = new DistrictVar[d.Count];
                for (int i = 0; i < d.Count; i++)
                {
                    d.districtVars[i] = new DistrictVar(d, site, isInteger);
                    districtVars.Add(d.districtVars[i]);
                }
            }
        }

        ///*****����Gurobi*****
        public void runGRB(string mode,double areaK,double distK)
        {
            try
            {
                GRBEnv env = new GRBEnv(true);
                env.Set("LogFile", "mip1.log");
                env.Start();

                GRBModel model = new GRBModel(env);
                SetBasicVar(model);

                //Ŀ��
                GRBQuadExpr objExpr = new GRBQuadExpr();
                objExpr.Add(basicObj(model, areaK, distK));
                if (mode.Contains("Core")) 
                {
                    Console.WriteLine("������ģʽ");
                    objExpr.Add(CoreObj(model));
                }
                if (mode.Contains("Axis"))
                {
                    Console.WriteLine("����ģʽ");
                    AxisObj(model);
                }
                if (mode.Contains("Group"))
                {
                    Console.WriteLine("����ģʽ");
                    objExpr.Add(GroupObj(model));
                }
                if (mode.Contains("Grid"))
                {
                    Console.WriteLine("����ģʽ");
                    GridObj(model);
                }
                model.SetObjective(objExpr, GRB.MINIMIZE);

                //�Ż�����
                Settings(model);
                model.Optimize();

                //����
                setLoc(model);
                Console.WriteLine("Obj: " + model.ObjVal);

                // �ر�ģ��
                model.Dispose();
                env.Dispose();
            }
            catch (GRBException e)
            {
                Console.WriteLine("Error code: " + e.ErrorCode + ". " + e.Message);
            }
        }

        #region ����ģʽ���ⷽ��
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

        #region �������������ơ�Ŀ��
        protected void SetBasicVar(GRBModel model)  
        {
            //������ʼ����
            foreach(DistrictVar dv in districtVars)
            {
                dv.SetVar(model);
                dv.SiteConstr(model);
                model.AddConstr(dv.dx >= lengthMin, "");
                model.AddConstr(dv.dy >= lengthMin, "");
            }

            foreach (IDistrict d in districts)
            {
                //�������ߴ����
                if (d.Building_area == 0)
                {
                    model.AddConstr(d.districtVars[0].dx >= sportX / grid, " ");
                    model.AddConstr(d.districtVars[0].dy >= sportY / grid, " ");
                }
            }

            AreaConstr(model);

            for (int i = 0; i < dvCount - 1; i++)
            {
                
                districtVars[i].ConstrOverlap(model, districtVars, spacing);
            }
        }

        private void AreaConstr(GRBModel model) {
            GRBQuadExpr expr= new GRBQuadExpr();
            GRBQuadExpr expr1 = new GRBQuadExpr();

            ///�����������
            foreach (IDistrict d in districts)
            {
                //����ȡ�����Ҫ�� 
                foreach (DistrictVar dv in d.districtVars)
                {
                    expr.Add(dv.dx * dv.dy);//�������������
                }
                //���õ�����Χor�����ܸ���
                if (d.area_lim == null)
                {
                    model.AddQConstr(expr >= d.S, "dx*dy>area");
                }
                else
                {
                    model.AddQConstr(expr <= d.area_lim.max, "area<area_max");
                    model.AddQConstr(expr >= d.area_lim.min, "area>area_min");
                }

                //���
                expr.Clear();
            }

            ///�����������
            foreach (DistrictVar dv in districtVars)
            {
                expr1.AddTerm(1, dv.dx, dv.dy);
                if (dv.District.building_area > 0)
                    expr.AddTerm(1, dv.dx, dv.dy);     
            }
            if (totalAreaLimit)
                model.AddQConstr(expr >= totalArea / grid / grid, " "); //У�����õ�����ﵽҪ��

            if (layoutDensity > 0)
                model.AddQConstr(expr1 >= layoutDensity * site.Area(), " S_totall> r%*site.area");
        }

        protected GRBQuadExpr basicObj(GRBModel model,double areaK,double distK)
        {
            var basicObj = new GRBQuadExpr();
            //*���
            if (areaK > 0)
            {
                GRBQuadExpr expr = new GRBQuadExpr();
                foreach (DistrictVar dv in districtVars)
                {
                    ///���ռ���ʣ�0��100��
                    if (dv.District.building_area > 0)
                        expr.AddTerm(1, dv.dx, dv.dy);
                }
                double k = areaK * 100;
                double total = site.Area();
                Console.WriteLine("���������=" + total);
                basicObj.Add((k / total) * (total - expr));
                expr.Clear();
            }
            //*����
            if (distK == 0)
            {return basicObj;  }

            //�����ص��߾���
            foreach (DistrictVar dv in districtVars)
            {
                ///����ָ�꣨0��100��
                if (dv.sigleP_link != null && dv.sigleP_link.Count > 0)
                    dv.SetEntranceObj(basicObj, distK * 100 / dv.sigleP_link.Count);
                if (dv.road_link != null && dv.road_link.Count > 0)
                    dv.SetRoadObj(basicObj, 1);
                if (dv.road_align != null && dv.road_align.Count > 0)
                    dv.SetRoadConstr(model);
            }
            //��������
            if (districtLink != null)
            {
                foreach (int[] ds in districtLink)
                {
                    var district1 = districts[ds[0]];
                    var district2 = districts[ds[1]];
                    int c = district2.Count;
                    if (district1.Count < district2.Count)
                        c = district1.Count;
                    for (int i = 0; i < c; i++)
                    {
                        district1[i].DistrictLinkObj(basicObj, district2[i]);
                    }

                }
            }
            if (districtDist != null)
            {
                foreach(int i in districtDist)
                {
                    IDistrict d = districts[i];
                    int c = d.Count;
                    for (int j = 0; j< c-1; j++)
                    {
                        
                        d.districtVars[j].DistrictDistObj(basicObj, d.districtVars[j+1]);
                    }
                }
            }
            return basicObj;
        }

        protected void Settings(GRBModel model)
        {
            model.Set(GRB.IntParam.NonConvex, 2);
            model.Set(GRB.IntParam.PoolSearchMode, poolSearchMode);//default:
            //model.Set(GRB.DoubleParam.PoolGap, 3);
            model.Set(GRB.IntParam.PoolSolutions, resultCount);
            model.Set(GRB.DoubleParam.TimeLimit, time);
        }
        protected virtual void setLoc(GRBModel model)
        {
        }
        #endregion


        #region Topology
        public void PointLink(int index1,int index2,string entrances)
        {
            for (int i = 0; i < site.Entrances.Count; i++)
            {
                if (entrances.Contains(i.ToString()))
                {
                    districts[index1].districtVars[index2].sigleP_link.Add(i);
                }
            }
        }
        public void PointLink(int index1, string entrances)
        {
            for (int i = 0; i < site.Entrances.Count; i++)
            {
                if (entrances.Contains(i.ToString()))
                {
                    foreach(DistrictVar dv in districts[index1].districtVars) {
                        dv.sigleP_link.Add(i);
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
                    districts[index1].districtVars[index2].road_link.Add(i);
                }
            }
        }

        public void RoadLink(int index1, string roads)
        {
            for (int i = 0; i < site.Roads.Count; i++)
            {

                if (roads.Contains(i.ToString()))
                {

                    foreach (DistrictVar dv in districts[index1].districtVars)
                    {
                        
                        dv.road_link.Add(i);
                    }
                }
            }
        }

        public void RoadAlign(int index1, int index2, string roads, double tolerance)
        {
            districts[index1].districtVars[index2].align_tolerance = tolerance;
            for (int i = 0; i < site.Roads.Count; i++)
            {
                if (roads.Contains(i.ToString()))
                {
                    districts[index1].districtVars[index2].road_align.Add(i);
                }
            }
        }
        public void RoadAlign(int index1,  string roads, double tolerance)
        {
            foreach(DistrictVar dv in districts[index1].districtVars)
            {
                dv.align_tolerance = tolerance;
            }
            
            for (int i = 0; i < site.Roads.Count; i++)
            {
                if (roads.Contains(i.ToString()))
                {
                    foreach (DistrictVar dv in districts[index1].districtVars)
                    {
                        dv.road_align.Add(i);
                    }
                }
            }
        }

        public void RoadSingleSide(string districtIndex, int road, int side)
        {
            try
            {
                var indexes = districtIndex.Split(',');
                for (int i = 0; i < districts.Count; i++)
                {
                    if (indexes.Contains(i.ToString()))
                    {
                        foreach (DistrictVar dv in districts[i].districtVars)
                        {
                            dv.roadSide.Add(new int[] { road, side });
                            //Console.WriteLine($"{road},{side}");
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("RoadSingleSideѡȡʧ��");
            }

        }

        public void DistrictLink(int district1,int district2)
        {
            districtLink.Add(new int[] { district1, district2 });
        }

        public void Spacing(double dist)
        {
            spacing = dist / 20;
        }

        public void DistrictDist(int index)
        {
            districtDist.Add(index);
        }

        #endregion

        #region �ߴ����
        public void LenToWidth(double lenToWidth){
            foreach (DistrictVar dv in districtVars)
            {
                dv.lenToWidth = lenToWidth;
            }
        }

        //���������
        public void AreaFloats(double scale1,double scale2)
        {
            foreach (IDistrict d in districts)
            {
                d.area_lim = new Domain(scale1 * d.S, scale2 * d.S);
            }
        }
        public void AreaFloats(double scale1)
        {
            foreach (IDistrict d in districts)
            {
                d.Site_area = scale1 * d.Site_area;
            }
        }

        //�������������
        public void AreaFloats(int index, double deta1, double deta2)
        {
            districts[index].area_lim = new Domain( deta1 * districts[index].S, deta2* districts[index].S);
        }

        //�ӷ���
        public void AreaSep(int index, double k)
        {
            if (k > 1 || k < 0)
            {
                return;
            }
            if (districts[index].Count > 1)
            {
                foreach(DistrictVar dv in districts[index].districtVars)
                {
                    dv.area_lim.min = k * districts[index].S / districts[index].Count;
                }
            }
        }
        public void AreaSep(double k)
        {
            if (k > 1 || k < 0)
            {
                return;
            }
            foreach (IDistrict d in districts)
            {
                if (d.Count > 1&&d.Building_area>0)
                {
                    foreach (DistrictVar dv in d.districtVars)
                    {
                        dv.area_lim.min = k* d.S/d.Count ;
                    }
                }
            }
        }
        public void LengthMin(double min)
        {
            if (min > 0)
            {
                lengthMin = min/grid;
            }
        }
        public void LengthMin(int index,double min)
        {
            if (min > 0)
            {
                districts[index].Length_min = min/grid;
            }
        }
        public void SportArea(double X,double Y)
        {
            sportX = X;
            sportY = Y;
        }

        //���������
        public void TotalAreaLimit()
        {
            totalAreaLimit = true;
        }
        public void TotalAreaLimit(double area)
        {
            totalAreaLimit = true;
            totalArea = area;
        }

        //���÷������ֵ��ܶ�
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

        #region ��װ
        public string SiteCsv { set => SiteCsv = value; }
        //public string DistrictsCsv { set => districtsCsv = value; }
        public int IsInteger { set => isInteger = value; }
        public List<IDistrict> Districts { get => districts; }
        public Site Site { get => site; }

        public float[] AreaResult { get => areaResult; }
        public int ResultCount { set => resultCount = value; }
        public int PoolSearchMode { set => poolSearchMode = value; }

        public List<DistrictVar> DistrictVars { get => districtVars; set => districtVars = value; }
        public double Time { get => time; set => time = value; }
        #endregion

    }
}