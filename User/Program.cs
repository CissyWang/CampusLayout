using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flowing;
using OpenTK;
using InitialArrange;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using IndexCalculate;
/// <summary>
/// 问题记录
/// *1.boundry保留两位小数比保留一位要快（结果不一样，数值差不多）
/// *2.minus超出范围计算不出来
/// *3.不同的searchMode计算结果和耗时不一样：1最慢，0中等，2最快（由最优到满足限制）
/// （0专注最优解，1按数量搜索可行解并不保证质量，2系统搜索可行解）
/// *4.对面积（总面积）限制合理可以算得更快
/// *5 目标如果较难实现也会耗时过长长长 
/// </summary>

namespace User
{
    class Program : IApp
    {
        #region 
        static void Main(string[] args)
        {
            main();
        }
        Calculator myCal;
        CamController cam;
         int resultN = 0;//当前解
        bool result = false;
        Font font;
        int bgColor = 230;
        
        Campus campus; 
        
        int grid = 20;             //运行单元大小
        bool info=false;
        bool build = false;
        #endregion

        #region FilePath
        string mustFile = "../南工职大/应用/mustBuilding.csv";
        string optionalFile = "../南工职大/应用/optionalBuilding1.csv";
        string exportPath = "../南工职大/应用/export1.2.csv";

        string siteCsv = "../南工职大/应用/site0.csv";//场地信息
        string export = "../南工职大/应用/export1.2-0.csv";//分区信息

        #endregion
        string locationCsv = "../南工职大/应用/2-locationT.csv";//分区位置

        int resultCount = 5; //解数
        double time =200.0;//
        int searchMode = 2;// 模式：最大时用2 否则用0
        
        override
        public void SetUp()
        {
            #region
            //Console.WriteLine("请依次输入以下信息：");
            //Console.WriteLine("（1）建设校园类型，" +
            //    "(0综合一类 , 1工业类， 2财经、政法、管理类, 3体育类，4综合二类、师范类， 5农林、医药类，6外语类，7艺术类)");
            //Console.WriteLine("（2）建设用地（公顷）");
            //Console.WriteLine("（3）学校规模（学生数：人）");
            //Console.WriteLine("（4）容积率");


            //int type = Convert.ToInt32(Console.ReadLine());
            //while (!Enum.IsDefined(typeof(schoolType), type))
            //{
            //    Console.WriteLine("不存在该学校类型，请重新输入建设校园类型");
            //    type = Convert.ToInt32(Console.ReadLine());
            //}

            //double area = Convert.ToDouble(Console.ReadLine());
            //int pop = Convert.ToInt32(Console.ReadLine());
            //double r = Convert.ToDouble(Console.ReadLine());

            //campus = new Campus((schoolType)type, pop, area, r, mustFile, optionalFile, exportPath);
            campus = new Campus(schoolType.工业类, 15000, 74.4, 1.2,mustFile,optionalFile,exportPath);
            campus.Run();
            //campus.Export();
            //myCal = new Calculator(grid, siteCsv,districtCsv);
            myCal = new Calculator(grid, siteCsv, campus,export);

            myCal.ResultCount = resultCount;
            myCal.PoolSearchMode = searchMode;
            myCal.Time = time;
            myCal.IsInteger = 0;
            #endregion

            #region 尺寸控制
            myCal.LenToWidth(2f);//长宽比（默认为3.0）
            myCal.Spacing(12);//间距

            myCal.AreaFloats(1.0,1.32);//面积浮动范围
            int[] aa;

            myCal.AreaSep(0.7);//校舍
            myCal.AreaSep(19,0.5);//体育
            myCal.SportArea(150, 230);//运动场0长宽
            myCal.LengthMin(60);//最小长宽

            //myCal.TotalAreaLimit(campus.Building_siteArea);
            //myCal.SetLayoutDensity(0.7);//设置总场地占用；
            #endregion

            #region 2.设置位置关系

            //aa = new int[] { 0, 3, 5, 9,11 };
            //foreach(int i in aa)
            //{ myCal.PointLink(i, "4");}
            
            myCal.RoadSingleSide("13,14,16,17", 1, 0);//1下
            myCal.RoadSingleSide("15,18", 3, 1);//3上

            myCal.RoadSingleSide("1,2", 2, 0);//2下
            myCal.RoadSingleSide("10", 2, 1);//2上
            myCal.RoadLink(1, "0");
            myCal.RoadLink(6, "2");
            myCal.PointLink(6,0, "2");//
            myCal.RoadLink(19,0, "7");

            myCal.DistrictLink(4, 19);
            myCal.DistrictLink(6, 8);

            myCal.DistrictDist(8);
            #endregion

            #region 3.设置模式
            #region 3.中心区布局
            ////myCal.Core_domain(new int[] { 42, 44 }, new int[] { 34, 40}, new int[] { 13,16},new int[] { 16, 21 });
            //myCal.Core_domain(new int[] { 42, 44 },new int[] { 42, 44 }, new int[] { 18, 20 }, new int[] { 14, 16 });

            //myCal.Core_Inside("0,3,5,9,11", true, false);
            ////myCal.Core_Outside("1",true);
            //myCal.CoreK(-1);
            //myCal.Core_width(1);
            #endregion

            #region 轴线式布局
            //myCal.AddAxis( new Line[] { new Line(41.6f, 26, 41.6f, 59) });
            //myCal.VirtualAxis(0, new int[] { 0, 3, 5,11 },10,false);
            //myCal.RealAxis(1, new int[] { 3 },3);
            #endregion

            #region 多组团式布局
            //myCal.SetGroup(0, new int[] {0,5,3,9 }, 0.3);
            //myCal.SetGroup(1, new int[] { 3 }, 0.3);//增加Domain

            #endregion

            #region 网格式布局
            //myCal.AddGrid(new Domain[] { new Domain(44, 45), new Domain(52, 60) }, 0.5f, 1);//横向
            //myCal.AddGrid(new Domain[] { new Domain(36, 37), new Domain(23.5, 32) }, 0.5f, 1);//横向
            //myCal.AddGrid(new Domain[] { new Domain(41, 42), new Domain(26, 35) }, 0.5f, 0);//纵向

            #endregion
            #endregion

            Console.ReadLine();
            myCal.runGRB("", 0,0);
            //myCal.runGRB("Core,Grid", 1, 1);
            myCal.ResponseExportCSV(locationCsv);//导出表格

            #region 显示设置
            //Smooth(8);
            Size(800, 600);
            cam = new CamController(this);
            cam.FixZaxisRotation = true;
            ColorMode(HSB);
            font = CreateFont("微软雅黑", 24);
            TextFont(font,0.5f*grid);
            #endregion
        }

        override
        public  void Draw()
        {
            TextAlign(1, 1);
            Background(255);
            //cam.DrawSystem(this, 200);//画网格

            if (!result)
            {
                campus.InitialResult(this);
            }
            else
            {
                DrawSiteElements();
                DrawDistrict();
                DrawStructure();
                DrawBoundry();
                this.ShowIndex();
                
            }

        }

        public override void KeyReleased()
        {
            if (key == "Right")
            {
                resultN = (resultN + 1) % resultCount;
            }
            else if (key == "Left")
            {
                resultN = (resultN - 1 + resultCount) % resultCount;
            }
            if (key == "Space")
            {
                result = !result;
            }
            if (key == "I")
            {
                info = !info;
            }
            if (key == "B")
            {
                build = !build;
            }
        }

        #region 
        public void ShowIndex()
        {
            #region 注释
            Fill(0);
            TextAlign(0, 0);
            Text(resultN + "用地" + myCal.AreaResult[resultN], 0, 0, 0);
            Text("占地" + myCal.Site.Area() * grid * grid, 0, 3 * grid, 0);
            # endregion
        }

        public void DrawDistrict()
        {
            StrokeWeight(2);
            StrokeJoin(3);
            int index1 = 0;//colour
            int index2 = 0;
            
            foreach (DistrictVar dv in myCal.DistrictVars)
            {
                index1 = dv.District.Index;
                index2 = dv.Index;

                ///show rectangle
                Fill(10 + 12 * index1, 120, 235);
                //Stroke(50+17* index, 220, 150);
                Stroke(255);

                var rect = dv.rectResults[resultN];
                BeginShape();
                Vertex(grid * rect.X1, grid * rect.Y1, 0);
                Vertex(grid * rect.X2, grid * rect.Y1, 0);
                Vertex(grid * rect.X2, grid * rect.Y2, 0);
                Vertex(grid * rect.X1, grid * rect.Y2, 0);
                EndShape();

                ///show name
                Fill(0);
                string name = index1+dv.District.name + "-" + index2;
                if (index2 == 0)
                    name = index1 + dv.District.name;
                
                TextSize(0.5f * grid);
                TextAlign(1, 1);
                Text(name, grid * rect.Center.p, grid * rect.Center.q, 0);

                ///show information
                if (info)
                {
                    double siteArea = Math.Round(dv.Area(resultN, grid), 0);
                    double buildingArea = Math.Round(dv.BuildingArea(resultN), 0);
                    TextSize(0.35f * grid);
                    Text($"占地：{siteArea}", grid * rect.Center.p, grid * (rect.Center.q - 1), 0);
                    Text($"建筑：{ buildingArea}", grid * rect.Center.p, grid * (rect.Center.q - 1.7f), 0);

                }
            }
            
            if (!build)
            { return; }
            foreach (IDistrict d in myCal.Districts)
            {

                if (d.Buildings == null)
                {
                    continue;
                }

                Fill(10 + 12 *d.Index, 120, 235);
                float Dz = 4;
                float deta = 0.5f;
                for (int i = 0; i < d.Count; i++)
                {
                    var rect = d[i].rectResults[resultN];
                    float x = grid * rect.Center.p;
                    float y = grid * (rect.Y1 + deta);
                    float z = Dz/2;
                    float w = grid * (rect.Dx - 2*deta);

                    foreach (double[] info in d[i].BuildingInfo(resultN))
                    {
                        double floor_area = info[0];
                        float h = (float)floor_area / (rect.Dx - 2*deta) / grid;
                        y += h / 2;

                        for (int j = 0; j < info[1]; j++)
                        {
                            PushMatrix();
                            Translate( x, y,  z);
                            Cube(w, h, Dz);
                            z += Dz;
                            PopMatrix();
                        }
                        y += h / 2 + deta*grid;
                    }
                }
            }
        }
        public void DrawSiteElements()
        {
            var entrs = myCal.Site.Entrances;
            var roads = myCal.Site.Roads;
            //绘制Blocks
            Fill(180);
            NoStroke();
            if (myCal.Site.Blocks != null)
            {
                foreach (IRectangle m in myCal.Site.Blocks)
                {
                    BeginShape();
                    Vertex(grid * m.X1, grid * m.Y1, 0);
                    Vertex(grid * m.X2, grid * m.Y1, 0);
                    Vertex(grid * m.X2, grid * m.Y2, 0);
                    Vertex(grid * m.X1, grid * m.Y2, 0);
                    EndShape();
                }
            }
            //绘制minus
            Fill(250);
            if (myCal.Site.Minus != null)
            {
                foreach (IRectangle m in myCal.Site.Minus)
                {
                    BeginShape();
                    Vertex(grid * m.X1, grid * m.Y1, 0);
                    Vertex(grid * m.X2, grid * m.Y1, 0);
                    Vertex(grid * m.X2, grid * m.Y2, 0);
                    Vertex(grid * m.X1, grid * m.Y2, 0);
                    EndShape();
                }
            }
            //绘制入口
            Fill(0);
            if (entrs != null)
            {
                foreach (IPoint poi in entrs)
                {
                    PushMatrix();
                    //TextSize(grid);
                    Translate(grid * poi.p, grid * poi.q);
                    TextAlign(0, 0);
                    Text(entrs.IndexOf(poi).ToString(), 0, 0, 0);
                    Sphere(0.3f * grid);
                    PopMatrix();
                }
            }

            //绘制在外点
            Fill(0, 240, 240);
            if (myCal.Site.OutOfSite != null)
            {
                foreach (IPoint poi in myCal.Site.OutOfSite)
                {
                    PushMatrix();
                    //TextSize(grid);
                    Translate(grid * poi.p, grid * poi.q);
                    Sphere(0.2f * grid);
                    PopMatrix();
                }
            }

            //绘制道路
            Fill(0, 255, 200);//text
            if (roads != null)
            {
                foreach (Road r in roads)
                {
                    if (r.Width > 0)
                    {
                        StrokeWeight(grid * r.Width);
                        Stroke(200);
                        Line(grid * r.X1, grid * r.Y1, 0, grid * r.X2, grid * r.Y2, 0);
                        StrokeWeight(1); Stroke(0, 255, 255);
                        Line(grid * r.X1, grid * r.Y1, 0, grid * r.X2, grid * r.Y2, 0);
                        Text(roads.IndexOf(r).ToString(), (grid * r.X1 + grid * r.X2) / 2, (grid * r.Y1 + grid * r.Y2) / 2, 0);
                    }
                }
            }

        }
        public void DrawStructure()
        {

            //绘制中心区
            if (myCal.Core != null)
            {
                NoFill();
                Stroke(200);
                StrokeWeight(grid * 0.3f);
                var rect = myCal.Core.rectResults[resultN];
                BeginShape();
                Vertex(grid * rect.X1, grid * rect.Y1, 0);
                Vertex(grid * rect.X2, grid * rect.Y1, 0);
                Vertex(grid * rect.X2, grid * rect.Y2, 0);
                Vertex(grid * rect.X1, grid * rect.Y2, 0);
                EndShape();
            }
            //绘制组团
            if (myCal.Groups != null)
            {
                NoFill();
                Stroke(200);
                StrokeWeight(grid * 0.3f);
                foreach (DistrictVar dv in myCal.Groups)
                {
                    var rect = dv.rectResults[resultN];
                    BeginShape();
                    Vertex(grid * rect.X1, grid * rect.Y1, 0);
                    Vertex(grid * rect.X2, grid * rect.Y1, 0);
                    Vertex(grid * rect.X2, grid * rect.Y2, 0);
                    Vertex(grid * rect.X1, grid * rect.Y2, 0);
                    EndShape();
                }
            }
            // 绘制轴线
            if (myCal.Axis != null)
            {
                foreach (Line r in myCal.Axis)
                {
                    StrokeWeight(1);
                    Line(grid * r.X1, grid * r.Y1, 0, grid * r.X2, grid * r.Y2, 0);
                }
            }

            myCal.Grids(resultN, this);


        }

        public void DrawBoundry()
        {
            var boundry = myCal.Site.Boundry;
            NoFill();
            StrokeWeight(2);
            Stroke(0);
            BeginShape();
            foreach (IPoint p in boundry)
            {
                Vertex(grid * p.p, grid * p.q, 0);
            }
            EndShape();
        }
        #endregion

    }
}
