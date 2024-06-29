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
         int resultN = 0;//当前解
        bool result = false;
        Font font;
        bool build=false;

        int resultCount = 5; //解数
        int unit= 20;             //运行单元大小
        bool info=false;
        #endregion

        string siteCsv = "E:/grasshopper_C#/test/4.1/site3N.csv";//输入 - 场地信息
        string districtCsv = "E:/grasshopper_C#/test/4.1/export.csv";//输入 - 分区信息
        string locationCsv = "E:/grasshopper_C#/test/4.1/location3N.csv";//分区位置
        double time =30;
        int searchMode =2;

        override
        public void SetUp()
        {
            #region***初始化设置***
            myCal = new Calculator(unit, siteCsv, districtCsv, locationCsv)
            {
                ResultCount = resultCount,
                PoolSearchMode = searchMode,
                Time = time,
                IsInteger = 0
            };
            #endregion

            #region 尺寸控制
            //myCal.AreaFloats(1.0,1.4);//设置父分区的面积范围
            //myCal.SetLayoutDensity(0.7);//设置总场地占用；
            myCal.LengthMin(60);
            myCal.SetSpacing(8);//间距
            #endregion

            ///***启动运行***

            myCal.runGRB("",1, 0);
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
                    Vertex(unit * m.X1, unit * m.Y1, 0);
                    Vertex(unit * m.X2, unit * m.Y1, 0);
                    Vertex(unit * m.X2, unit * m.Y2, 0);
                    Vertex(unit * m.X1, unit * m.Y2, 0);
                    EndShape();
                }
            }

            //绘制入口
            Fill(0);
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
                       // Text(roads.IndexOf(r).ToString(), (unit * r.X1 + unit * r.X2) / 2, (unit * r.Y1 + unit * r.Y2) / 2, 0);
                    }
                }
            }
            //boundary
            Stroke(0);
            StrokeWeight(0.2f*unit);
            BeginShape();
                foreach (IPoint p in boundry)
            {
                Vertex(unit*p.p,unit* p.q, 0);
            }
            EndShape();
            #endregion

            #region 绘制分区
            StrokeWeight(2);
            StrokeJoin(3);
            int index1 = 0;//colour
            int index2 = 0;


            foreach (ZoneVar dv in myCal.ZoneVars)
            {

                index1 = myCal.Zones.IndexOf(dv.Zone);
                index2 = dv.Index;
                //Fill((25 + 23 * index1) % 255, 70, 245);
                Fill(200);
                NoStroke();
                //Stroke(50+17* index, 220, 150);
                // Stroke((25 + 23 * index1) % 255, 220, 145);
                var rect = dv.rectResults[resultN];
                BeginShape();
                Vertex(unit * rect.X1, unit * rect.Y1, 0);
                Vertex(unit * rect.X2, unit * rect.Y1, 0);
                Vertex(unit * rect.X2, unit * rect.Y2, 0);    
                Vertex(unit * rect.X1, unit * rect.Y2, 0);
                EndShape();

                //Fill((25 + 23 * index1) % 255, 220, 145);
                Fill(0);

                string name = dv.Zone.name+index2;
                if (index2 == 0)
                    name = dv.Zone.name;
                TextSize(1f * unit);
                TextAlign(1, 1);
                Text(index1.ToString(), unit * rect.Center.p, unit * rect.Center.q, 0);//文字
                
                if (info)
                {
                    double siteArea = Math.Round(dv.Area(resultN,unit), 0);
                    double buildingArea = Math.Round(dv.BuildingArea(resultN), 0);
                    TextSize(0.35f * unit);
                    Text($"占地：{siteArea}", unit * rect.Center.p, unit * (rect.Center.q - 1), 0);
                    Text($"建筑：{ buildingArea}", unit * rect.Center.p, unit * (rect.Center.q - 1.7f), 0);
                    
                }
            }
            #endregion

            if (!build)
            { return; }

            foreach (IZone d in myCal.Zones)
            {

                if (d.Buildings == null)
                {
                    continue;
                }

                Fill(10 + 12 * d.Index, 120, 235);
                float Dz = 4;
                float deta = 0.35f;
                Stroke(0);
#pragma warning disable CS0019 // 运算符“<”无法应用于“int”和“方法组”类型的操作数
                for (int i = 0; i < d.Count; i++)
#pragma warning restore CS0019 // 运算符“<”无法应用于“int”和“方法组”类型的操作数
                {
                    var rect = d[i].rectResults[resultN];
                    float x = unit * rect.Center.p;
                    float y = unit * (rect.Y1 + deta);
                    float z = Dz / 2;
                    float w = unit * (rect.Dx - 2 * deta);

                    foreach (double[] info in d[i].BuildingInfo(resultN))
                    {
                        double floor_area = info[0];
                        float h = (float)floor_area / (rect.Dx - 2 * deta) / unit;
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
                        y += h / 2 + deta * unit;
                    }
                }
            }

            #region 呈现结构
            //绘制中心区
            if (myCal.CoreVar != null)
            {
                NoFill();
                Stroke(200);
                StrokeWeight(unit*0.3f);
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
#pragma warning disable CS0246 // 未能找到类型或命名空间名“ZoneVar”(是否缺少 using 指令或程序集引用?)
                foreach (ZoneVar dv in myCal.GroupVars)
#pragma warning restore CS0246 // 未能找到类型或命名空间名“ZoneVar”(是否缺少 using 指令或程序集引用?)
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

            #endregion

            #region 绘制Boundry
            NoFill();
            StrokeWeight(2);
            Stroke(0);
            BeginShape();
            foreach (IPoint p in boundry)
            {
                Vertex(unit * p.p, unit * p.q, 0);
            }
            EndShape();
            #endregion

        }

        public void ShowIndex()
        {
            #region 注释
            Fill(0);
            TextSize(0.5f * unit);
            TextAlign(0, 0);
           Text(resultN + "用地" + myCal.AreaResult[resultN], -50, 0, 0);
            Text("占地" + myCal.Site.Area() * unit * unit, -50, 3 * unit, 0);
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
