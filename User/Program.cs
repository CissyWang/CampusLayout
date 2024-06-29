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
        
        int unit = 20;             //运行单元大小
        bool info=false;
        bool build = false;
        #endregion

        #region FilePath
        //用于计算指标，上一个程序需要修改为导出json，（csv可选），读取之后交互输入数量
        string mustFile = "../../南工职大/应用/mustBuilding.csv";
        string optionalFile = "../../南工职大/应用/optionalBuilding1.csv";
        string exportPath = "../../南工职大/应用/export1.2.csv";//分区信息(无子分区数量）


        string siteCsv = "../../南工职大/应用/site0.csv";//场地信息
        string export = "../../南工职大/应用/export1.2-0.csv";//分区信息(有子分区数量）

        #endregion
        string locationCsv = "../../南工职大/应用/2-locationT.csv";//分区位置

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
            //myCal = new Calculator(unit, siteCsv,districtCsv);
            myCal = new Calculator(unit, siteCsv, campus, export, locationCsv)
            {
                ResultCount = resultCount,
                PoolSearchMode = searchMode,
                Time = time,
                IsInteger = 0
            };
            #endregion

            #region 尺寸控制
            myCal.LenToWidth(2f);//长宽比（默认为3.0）
            myCal.SetSpacing(12);//间距

            myCal.AreaFloats(1.0,1.32);//面积浮动范围

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
         
            myCal.SetZoneLink(4, 19);
            myCal.SetZoneLink(6, 8);
            myCal.SetZoneDist(8);
            #endregion

            #region 3.设置模式
            #region 3.中心区布局
            ////myCal.SetCore_domain(new int[] { 42, 44 }, new int[] { 34, 40}, new int[] { 13,16},new int[] { 16, 21 });
            //myCal.Core_domain(new int[] { 42, 44 },new int[] { 42, 44 }, new int[] { 18, 20 }, new int[] { 14, 16 });

            //myCal.Core_Inside("0,3,5,9,11", true, false);
            //myCal.Core_Outside("1",true);
            //myCal.CoreK(-1);
            //myCal.Core_width=1;
            #endregion

            #region 轴线式布局
            //myCal.AddAxis( new Axis(41.6f, 26, 41.6f, 59) );
            //myCal.VirtualAxis(0, new int[] { 0, 3, 5,11 },10,false);
            //myCal.RealAxis(1, new int[] { 3 },3);
            #endregion

            #region 多组团式布局
            //myCal.SetGroup(0, new int[] {0,5,3,9 }, 0.3);

            #endregion

            #region 网格式布局
            //myCal.AddGrid(new Domain[] { new Domain(44, 45), new Domain(52, 60) }, 0.5f, 1);//横向
            //myCal.AddGrid(new Domain[] { new Domain(36, 37), new Domain(23.5, 32) }, 0.5f, 1);//横向
            //myCal.AddGrid(new Domain[] { new Domain(41, 42), new Domain(26, 35) }, 0.5f, 0);//纵向

            #endregion
            #endregion

            Console.ReadLine();
            myCal.runGRB("", 0,0);
            //myCal.runGRB("Core,Grids", 1, 1);
            myCal.ResponseExportCSV();//导出表格

            #region 显示设置
            //Smooth(8);
            Size(800, 600);
            cam = new CamController(this);
            cam.FixZaxisRotation = true;
            ColorMode(HSB);
            font = CreateFont("微软雅黑", 24);
            TextFont(font,0.5f*unit);
            #endregion
        }

        override
        public  void Draw()
        {
            TextAlign(1, 1);
            Background(bgColor);
            //cam.DrawSystem(this, 200);//画网格

            if (!result)
            {
                campus.InitialResult(this);
            }
            else
            {
                DrawSiteElements();
                DrawZone();
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
            Text("占地" + myCal.Site.Area() * unit * unit, 0, 3 * unit, 0);
            # endregion
        }

        public void DrawZone()
        {
            StrokeWeight(2);
            StrokeJoin(3);
            int index1 = 0;//colour
            int index2 = 0;
            

            foreach (ZoneVar dv in myCal.ZoneVars)
            {
                index1 = dv.Zone.Index;
                index2 = dv.Index;

                ///show rectangle
                Fill(10 + 12 * index1, 120, 235);
                //Stroke(50+17* index, 220, 150);
                Stroke(255);

                var rect = dv.rectResults[resultN];
                BeginShape();
                Vertex(unit * rect.X1, unit * rect.Y1, 0);
                Vertex(unit * rect.X2, unit * rect.Y1, 0);
                Vertex(unit * rect.X2, unit * rect.Y2, 0);
                Vertex(unit * rect.X1, unit * rect.Y2, 0);
                EndShape();

                ///show name
                Fill(0);
                string name = index1+dv.Zone.name + "-" + index2;
                if (index2 == 0)
                    name = index1 + dv.Zone.name;
                
                TextSize(0.5f * unit);
                TextAlign(1, 1);
                Text(name, unit * rect.Center.p, unit * rect.Center.q, 0);

                ///show information
                if (info)
                {
                    double siteArea = Math.Round(dv.Area(resultN, unit), 0);
                    double buildingArea = Math.Round(dv.BuildingArea(resultN), 0);
                    TextSize(0.35f * unit);
                    Text($"占地：{siteArea}", unit * rect.Center.p, unit * (rect.Center.q - 1), 0);
                    Text($"建筑：{ buildingArea}", unit * rect.Center.p, unit * (rect.Center.q - 1.7f), 0);

                }
            }
            
            if (!build)
            { return; }

            foreach (IZone d in myCal.Zones)
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
                    float x = unit * rect.Center.p;
                    float y = unit * (rect.Y1 + deta);
                    float z = Dz/2;
                    float w = unit * (rect.Dx - 2*deta);

                    foreach (double[] info in d[i].BuildingInfo(resultN))
                    {
                        double floor_area = info[0];
                        float h = (float)floor_area / (rect.Dx - 2*deta) / unit;
                        y += h / 2;

                        for (int j = 0; j < info[1]; j++)
                        {
                            PushMatrix();
                            Translate( x, y,  z);
                            Cube(w, h, Dz);
                            z += Dz;
                            PopMatrix();
                        }
                        y += h / 2 + deta*unit;
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
                    Vertex(unit * m.X1, unit * m.Y1, 0);
                    Vertex(unit * m.X2, unit * m.Y1, 0);
                    Vertex(unit * m.X2, unit * m.Y2, 0);
                    Vertex(unit * m.X1, unit * m.Y2, 0);
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
                    Vertex(unit * m.X1, unit * m.Y1, 0);
                    Vertex(unit * m.X2, unit * m.Y1, 0);
                    Vertex(unit * m.X2, unit * m.Y2, 0);
                    Vertex(unit * m.X1, unit * m.Y2, 0);
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
                    //TextSize(unit);
                    Translate(unit * poi.p, unit * poi.q);
                    TextAlign(0, 0);
                    Text(entrs.IndexOf(poi).ToString(), 0, 0, 0);
                    Sphere(0.3f * unit);
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
                    //TextSize(unit);
                    Translate(unit * poi.p, unit * poi.q);
                    Sphere(0.2f * unit);
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
                        StrokeWeight(unit * r.Width);
                        Stroke(200);
                        Line(unit * r.X1, unit * r.Y1, 0, unit * r.X2, unit * r.Y2, 0);
                        StrokeWeight(1); Stroke(0, 255, 255);
                        Line(unit * r.X1, unit * r.Y1, 0, unit * r.X2, unit * r.Y2, 0);
                        Text(roads.IndexOf(r).ToString(), (unit * r.X1 + unit * r.X2) / 2, (unit * r.Y1 + unit * r.Y2) / 2, 0);
                    }
                }
            }

        }
        public void DrawStructure()
        {

            //绘制中心区
            if (myCal.CoreVar != null)
            {
                NoFill();
                Stroke(200);
                StrokeWeight(unit * 0.3f);
                var rect = myCal.CoreVar.rectResults[resultN];
                BeginShape();
                Vertex(unit * rect.X1, unit * rect.Y1, 0);
                Vertex(unit * rect.X2, unit * rect.Y1, 0);
                Vertex(unit * rect.X2, unit * rect.Y2, 0);
                Vertex(unit * rect.X1, unit * rect.Y2, 0);
                EndShape();
            }
            //绘制组团
            if (myCal.Groups != null)
            {
                NoFill();
                Stroke(200);
                StrokeWeight(unit * 0.3f);
                foreach (ZoneVar dv in myCal.GroupVars)
                {
                    var rect = dv.rectResults[resultN];
                    BeginShape();
                    Vertex(unit * rect.X1, unit * rect.Y1, 0);
                    Vertex(unit * rect.X2, unit * rect.Y1, 0);
                    Vertex(unit * rect.X2, unit * rect.Y2, 0);
                    Vertex(unit * rect.X1, unit * rect.Y2, 0);
                    EndShape();
                }
            }
            // 绘制轴线
            if (myCal.Axes != null)
            {
                foreach (Line r in myCal.Axes)
                {
                    StrokeWeight(1);
                    Line(unit * r.X1, unit * r.Y1, 0, unit * r.X2, unit * r.Y2, 0);
                }
            }

            if (myCal.GridVars != null)
            {
                for (int i = 0; i < myCal.GridVars.Count; i++)
                {
                    StrokeWeight(myCal.GridVars[i].Stroke * unit);
                    var r = myCal.GridVars[i].ResultLine[resultN];
                    Line(unit * r.X1, unit * r.Y1, 0, unit * r.X2, unit * r.Y2, 0);
                }
            }


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
                Vertex(unit * p.p, unit * p.q, 0);
            }
            EndShape();
        }
        #endregion

    }
}
