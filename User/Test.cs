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
/// *5.排除点采用新方法更快
/// </summary>

namespace User
{
    class Test : IApp
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
        int resultCount = 8; //解数
        int grid = 20;             //运行单元大小
        bool info=false;
        #endregion

        string siteCsv = "../test/site1.csv";//场地信息
        string districtCsv = "../test/districtExportE1.csv";//分区信息
        string locationCsv = "../test/district_location11.csv";//分区位置
        double time = 200;

        override
        public void SetUp()
        {
              #region***初始化设置***
            myCal = new Calculator(grid, siteCsv,districtCsv);

            myCal.ResultCount = resultCount;
            myCal.PoolSearchMode =2;// （0专注最优解，1按数量搜索可行解并不保证质量，2系统搜索可行解）
            myCal.Time = time;

            ///***条件设置***
            myCal.IsInteger = 2;
            myCal.LenToWidth(2);//长宽比（默认为3.0）
            //myCal.Districts[10].SetArea_lim (20000, 0);//设置某种分区的面积范围

            //myCal.TotalArea(360000);//原定校舍面积算不出来
            //myCal.Density = 0.7;//设置总场地占用限制清楚更快；
            //设置最小长宽
           // myCal.LengthMin(9, 60);
            myCal.Spacing(0);
            myCal.AreaFloats(1,1.1);
            #endregion

            #region 2.设置位置关系
            //myCal.RoadLink(17, "0");
            //myCal.RoadAlign(8, 0, "3", 5);
            //myCal.PointLink(0, 0, "0");
            //myCal.DistrictLink(4, 18);

            for (int i = 0; i <10; i++)
            {
                myCal.PointLink(i, 0, "0");
            }
            //for (int i = 4; i < 7; i++)
            //{
            //    myCal.PointLink(i, 0, "2");
            //}
            //for (int i = 7; i < 10; i++)
            //{
            //    myCal.PointLink(i, 0, "0");
            //}
            #endregion


            ///***启动运行***
            #region 3.中心区布局
            //myCal.Core_domain(new int[] { 22, 26 }, new int[] { 21, 24 }, new int[] { 14, 18 }, new int[] { 16, 20 });//中心点位置
            //myCal.Core_Inside("0,3", true);//only or not
            //myCal.Core_Inside("all", true,false);
            //myCal.Core_Outside("5,6,7,9",false);
            //myCal.CoreK(1);
            //myCal.Core_width(1);
            ////myCal.runGRB("Core", 0, 1);
            #endregion

            #region 轴线式布局
            //myCal.Axis = new Line[] { new Line(10, 22, 25, 22), new Line(25, 6, 25, 40) };  
            //myCal.OnAxisDistricts = new int[][] { new int[] { 5 } , new int[] {  0,1 } };
            ////myCal.Axis_width(1);
            //myCal.InsideAxis(5,true);
            //myCal.runGRB("Axis", 0, 1);
            #endregion

            #region 多组团式布局
            //myCal.Group(0, new int[] { 0, 1, 5 }, 0.2);
            //myCal.Group(1, new int[] { 2,3,8 }, 0.3);//增加Domain

            //myCal.runGRB("Group", 0, 1);
            #endregion

            #region 网格式布局
            //myCal.AddGrid(new Domain[] { new Domain(24, 26), new Domain(30, 42) }, 0.2f, 0);//纵向
            //myCal.AddGrid(new Domain[] { new Domain(22, 25), new Domain(31, 44) }, 0.2f, 1);//横向
            //myCal.AddGrid(new Domain[] { new Domain(22, 25), new Domain(6, 17) }, 0.2f, 1);//横向
            #endregion


            myCal.runGRB("", 0, 1);
           // myCal.runGRB("Core", 0, 0);
            //myCal.runGRB("Core,Grid", 0, 1);
            //myCal.ResponseExportCSV(locationCsv);//导出表格

            #region 显示设置
            //Smooth(8);
            Size(800, 600);
            cam = new CamController(this);
            cam.FixZaxisRotation = true;
            ColorMode(HSB);
            //ColorMode(RGB);
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

            //if (!result)
            //    //campus.InitialResult(this);
            //else
            //{
                this.ShowResult();
                this.ShowIndex();
            //}
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
                if (entrs.IndexOf(poi) <3)
                {
                    PushMatrix();
                //TextSize(grid);
                Translate(grid * poi.p, grid * poi.q);
                TextAlign(0, 1);
                    TextSize(grid);
        
                   Text("P" + entrs.IndexOf(poi).ToString(), 0, -40, 0);
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
            #endregion
            
            #region 绘制分区
            StrokeWeight(2f);
            int index1 = 0;//colour
            int index2 = 0;

            foreach (DistrictVar dv in myCal.DistrictVars)
            {
                index1 = myCal.Districts.IndexOf(dv.District);
                index2 = dv.Index;
                Fill((25 +23  * index1)%255,70, 245);
                //Fill(colorList[index1,0], colorList[index1, 1], colorList[index1, 2]);
                //Stroke(50+17* index, 220, 150);
                Stroke(255);
                var rect = dv.rectResults[resultN];
                BeginShape();
                Vertex(grid * rect.X1, grid * rect.Y1, 0);
                Vertex(grid * rect.X2, grid * rect.Y1, 0);
                Vertex(grid * rect.X2, grid * rect.Y2, 0);    
                Vertex(grid * rect.X1, grid * rect.Y2, 0);
                EndShape();

                //Fill(150,150,150);
                Fill((25 + 23 * index1) % 255, 220, 145);
                string index = index1 + "-" + index2;
                if (index2 == 0)
                    index = index1.ToString();
                TextSize(1f * grid);
                TextAlign(1, 1);
                //Text(index + "." + dv.District.name, grid * rect.Center.p, grid * rect.Center.q, 0);//文字
                
                if (info)
                {
                    double buildingArea = dv.Area(resultN,grid) * dv.District.Building_area / dv.District.Result_area(resultN);
                    double siteArea = Math.Round(dv.Area(resultN,grid), 0);
                    buildingArea = Math.Round(buildingArea, 0);
                    TextSize(0.9f * grid);
                    double temp = Math.Round(0.05f * 0.05 * siteArea, 1);
                    Text(temp.ToString(), grid * rect.Center.p, grid * (rect.Center.q ), 0);
                    //Text("占地：" + temp.ToString(), grid * rect.Center.p, grid * (rect.Center.q - 1), 0);
                    //Text("建筑：" + buildingArea.ToString(), grid * rect.Center.p, grid * (rect.Center.q - 1.7f), 0);
                    
                }
            }
            #endregion

            #region 呈现结构
            //绘制中心区
            if (myCal.Core != null)
            {
                NoFill();
                Stroke(150,150,150);
                StrokeWeight(grid*0.1f);
                var rect = myCal.Core.rectResults[resultN];
                BeginShape();
                Vertex(grid * rect.X1, grid * rect.Y1, 0);
                Vertex(grid * rect.X2, grid * rect.Y1, 0);
                Vertex(grid * rect.X2, grid * rect.Y2, 0);
                Vertex(grid * rect.X1, grid * rect.Y2, 0);
                EndShape();
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
            //NoFill();
            //StrokeWeight(2);
            //Stroke(0);
            //BeginShape();
            //foreach (IPoint p in boundry)
            //{
            //    Vertex(grid * p.p, grid * p.q, 0);
            //}
            //EndShape();
            #endregion

        }

        public void ShowIndex()
        {
            #region 注释
            Fill(0);
            TextAlign(0, 0);
            Text(resultN + "用地" + myCal.AreaResult[resultN], 0, 0, 0);
            //Text("占地" + myCal.Site.Area() * grid * grid, 0, 3 * grid, 0);
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
