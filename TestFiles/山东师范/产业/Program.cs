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
        
        //Campus campus; 
        int resultCount = 5; //解数
        int grid = 20;             //运行单元大小
        bool info=false;
        #endregion
        //string mustFile = "../../../山东师范职业技术/mustBuilding.csv";
        //string optionalFile = "../../../山东师范职业技术/optionalBuilding.csv";
        //string exportPath = "../../../山东师范职业技术/districtExport.csv";

        string siteCsv = "../../../山东师范职业技术/Site1_road.csv";//场地信息
        string districtCsv = "../../../山东师范职业技术/产业/export11.csv";//分区信息
        string locationCsv = "../../../山东师范职业技术/产业/location_R4.csv";//分区位置
        double time = 20.0;
        int searchMode = 2;// （0专注最优解，1按数量搜索可行解并不保证质量，2系统搜索可行解）

        override
        public void SetUp()
        {
              #region***初始化设置***
            //campus = new Campus(schoolType.工业类, 13000, 62.4, 1,mustFile,optionalFile,exportPath);
            myCal = new Calculator(grid, siteCsv,districtCsv);

            myCal.ResultCount = resultCount;
            myCal.PoolSearchMode = searchMode;
            myCal.Time = time;
            myCal.IsInteger = 0;
            #endregion

            #region 尺寸控制
            myCal.LenToWidth = 2.0;//长宽比（默认为3.0）

            myCal.AreaFloats(1,1.15);//设置父分区的面积范围
            myCal.AreaFloats(0, -0.1, 0.2);
            myCal.AreaFloats(1, -0.1, 0.2);
            myCal.AreaFloats(2, -0.1, 0.2);
            //myCal.TotalArea(360000);
            //myCal.TotalArea();//达到生均校舍用地要求
            //myCal.SetLayoutDensity(0.7);//设置总场地占用；

            myCal.Spacing = 0.5;//间距

            //设置最小长宽
            myCal.SportArea(150, 200);
            myCal.Districts[3].Length_min = 120/grid;
            #endregion

            #region 2.设置位置关系
            myCal.PointLink(0, 0, "0");
            //myCal.RoadLink(3, "1");
            // myCal.RoadAlign(9, 0, "1", 5);
            myCal.PointLink(6, 0, "1");
            myCal.DistrictLink(12, 14, false);

            #endregion


            ///***启动运行***
            #region 3.中心区布局
            //myCal.Core_domain(new int[] { 24, 28 }, new int[] { 21, 28 }, new int[] { 4, 10 }, new int[] { 20, 24 });
            //myCal.Core_Inside("6,7,8,9", true,false);//only or not
            //////myCal.Core_Outside("5,6,7,9",false);
            //////myCal.CoreK(1);
            ////myCal.Core_width(1);
            #endregion

            #region 轴线式布局
            //myCal.Axis = new Line[] { new Line(39, 27, 46, 27)};  
            //myCal.OnAxisDistricts = new int[][] { new int[] {0,1,2 } , new int[] {  0 } };
            //myCal.Axis_width(1);
            ////myCal.InsideAxis(5,true);
            ////myCal.runGRB("Axis", 0, 1);
            #endregion

            #region 多组团式布局
            //myCal.SetGroup(0, new int[] {0,1,2 }, 0.3);
            //myCal.SetGroup(1, new int[] { 3 }, 0.3);//增加Domain

            #endregion

            #region 网格式布局
            ////myCal.AddGrid(new Domain[] { new Domain(24, 26), new Domain(30, 42) }, 0.2f, 0);//纵向
            myCal.AddGrid(new Domain[] { new Domain(8, 13), new Domain(0, 39) }, 0.5f, 1,
                new int[] { 10, 11, 12, 13, 14 }, 0);//横向
            #endregion

            myCal.runGRB("Grid", 1, 1);
            //myCal.runGRB("Axis,Group,Core,Grid", 1, 1);

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
            Background(bgColor);
            //cam.DrawSystem(this, 200);//画网格

            if (result)
            {
                //campus.InitialResult(this);
            }
            else
            {
                this.ShowResult();
                this.ShowIndex();
            }
        }

        public void ShowResult()
        {
            ///呈现场地信息
            var boundry = myCal.Site.Boundry;
            var entrs = myCal.Site.Entrances;
            var roads = myCal.Site.Roads;

            #region 呈现场地信息

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
            #endregion
            
            #region 绘制分区
            StrokeWeight(2);
            StrokeJoin(3);
            int index1 = 0;//colour
            int index2 = 0;

            foreach (DistrictVar dv in myCal.DistrictVars)
            {
                index1 = myCal.Districts.IndexOf(dv.District);
                index2 = dv.Index;
                Fill(50 + 17 * index1, 120, 235);
                //Stroke(50+17* index, 220, 150);
                Stroke(255);
                var rect = dv.rectResults[resultN];
                BeginShape();
                Vertex(grid * rect.X1, grid * rect.Y1, 0);
                Vertex(grid * rect.X2, grid * rect.Y1, 0);
                Vertex(grid * rect.X2, grid * rect.Y2, 0);    
                Vertex(grid * rect.X1, grid * rect.Y2, 0);
                EndShape();

                Fill(0);
                
                string name = dv.District.name + "-" + index2;
                if (index2 == 0)
                    name = dv.District.name;
                TextSize(0.5f * grid);
                TextAlign(1, 1);
                Text(name, grid * rect.Center.p, grid * rect.Center.q, 0);//文字
                
                if (info)
                {
                    double siteArea = Math.Round(dv.Area(resultN,grid), 0);
                    double buildingArea = Math.Round(dv.BuildingArea(resultN), 0);
                    TextSize(0.35f * grid);
                    Text($"占地：{siteArea}", grid * rect.Center.p, grid * (rect.Center.q - 1), 0);
                    Text($"建筑：{ buildingArea}", grid * rect.Center.p, grid * (rect.Center.q - 1.7f), 0);
                    
                }
            }
            #endregion

            #region 呈现结构
            //绘制中心区
            if (myCal.Core != null)
            {
                NoFill();
                Stroke(200);
                StrokeWeight(grid*0.3f);
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

            #endregion

            #region 绘制Boundry
            NoFill();
            StrokeWeight(2);
            Stroke(0);
            BeginShape();
            foreach (IPoint p in boundry)
            {
                Vertex(grid * p.p, grid * p.q, 0);
            }
            EndShape();
            #endregion

        }

        public void ShowIndex()
        {
            #region 注释
            Fill(0);
            TextAlign(0, 0);
            Text(resultN + "用地" + myCal.AreaResult[resultN], 0, 0, 0);
            Text("占地" + myCal.Site.Area() * grid * grid, 0, 3 * grid, 0);
            # endregion
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
            if(key == "I")
            {
                info = !info;
            }
        }

    }            
}
