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
    class SiteTest: IApp
    {
        #region 
        static void Main(string[] args)
        {
            main();
        }
        Calculator myCal;
        CamController cam;
        Campus campus;
         int resultN = 0;//当前解
        bool result = false;
        Font font;
        int bgColor = 230;
        bool build=false;

        int resultCount = 5; //解数
        int grid = 20;             //运行单元大小
        bool info=false;
        #endregion

        string siteCsv = "../test/4.1/site3N.csv";//场地信息
        string districtCsv = "../test/4.1/export.csv";//分区信息
        string locationCsv = "../test/4.1/location3N.csv";//分区位置
        double time =30;
        int searchMode =2;

        override
        public void SetUp()
        {
            #region***初始化设置***
            myCal = new Calculator(grid, siteCsv,districtCsv);

            myCal.ResultCount = resultCount;
            myCal.PoolSearchMode = searchMode;
            myCal.Time = time;
            myCal.IsInteger = 0;
            #endregion

            #region 尺寸控制
            //myCal.AreaFloats(1.0,1.4);//设置父分区的面积范围

            //myCal.SetLayoutDensity(0.7);//设置总场地占用；
            myCal.LengthMin(60);
            myCal.Spacing(8);//间距
            #endregion

            ///***启动运行***

            myCal.runGRB("",1, 0);

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
            this.ShowResult();
            this.ShowIndex();
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
                       // Text(roads.IndexOf(r).ToString(), (grid * r.X1 + grid * r.X2) / 2, (grid * r.Y1 + grid * r.Y2) / 2, 0);
                    }
                }
            }
            //boundary
            Stroke(0);
            StrokeWeight(0.2f*grid);
            BeginShape();
                foreach (IPoint p in boundry)
            {
                Vertex(grid*p.p,grid* p.q, 0);
            }
            EndShape();
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
                //Fill((25 + 23 * index1) % 255, 70, 245);
                Fill(200);
                NoStroke();
                //Stroke(50+17* index, 220, 150);
                // Stroke((25 + 23 * index1) % 255, 220, 145);
                var rect = dv.rectResults[resultN];
                BeginShape();
                Vertex(grid * rect.X1, grid * rect.Y1, 0);
                Vertex(grid * rect.X2, grid * rect.Y1, 0);
                Vertex(grid * rect.X2, grid * rect.Y2, 0);    
                Vertex(grid * rect.X1, grid * rect.Y2, 0);
                EndShape();

                //Fill((25 + 23 * index1) % 255, 220, 145);
                Fill(0);

                string name = dv.District.name+index2;
                if (index2 == 0)
                    name = dv.District.name;
                TextSize(1f * grid);
                TextAlign(1, 1);
                Text(index1.ToString(), grid * rect.Center.p, grid * rect.Center.q, 0);//文字
                
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

            if (!build)
            { return; }
            foreach (IDistrict d in myCal.Districts)
            {

                if (d.Buildings == null)
                {
                    continue;
                }

                Fill(10 + 12 * d.Index, 120, 235);
                float Dz = 4;
                float deta = 0.35f;
                Stroke(0);
                for (int i = 0; i < d.Count; i++)
                {
                    var rect = d[i].rectResults[resultN];
                    float x = grid * rect.Center.p;
                    float y = grid * (rect.Y1 + deta);
                    float z = Dz / 2;
                    float w = grid * (rect.Dx - 2 * deta);

                    foreach (double[] info in d[i].BuildingInfo(resultN))
                    {
                        double floor_area = info[0];
                        float h = (float)floor_area / (rect.Dx - 2 * deta) / grid;
                        y += h / 2;

                        //每层
                        for (int j = 0; j < info[1]; j++)
                        {
                            PushMatrix();
                            Translate(x, y, z);
                            Cube(w, h, Dz);
                            z += Dz;
                            PopMatrix();
                        }
                        z = Dz / 2;
                        y += h / 2 + deta * grid;
                    }
                }
            }

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
            TextSize(0.5f * grid);
            TextAlign(0, 0);
           Text(resultN + "用地" + myCal.AreaResult[resultN], -50, 0, 0);
            Text("占地" + myCal.Site.Area() * grid * grid, -50, 3 * grid, 0);
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
            if (key == "B")
            {
                build = !build;
            }
        }

    }            
}
