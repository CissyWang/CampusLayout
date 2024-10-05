//完整流程
using System;
using Flowing;
using InitialArrange;
using System.Drawing;
using CampusClass;

namespace User
{
    class ProgramNew : IApp
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
        bool info=false;
        bool build = false;
        Campus campus;
        static string fileName1 = "E:/grasshopper_C#/test/4.3/mustBuilding1.csv";
        static string fileName2 = "E:/grasshopper_C#/test/4.3/optionalBuilding1.csv";
        static string exportPath = "E:/grasshopper_C#/test/4.3/exportN.csv";
        #endregion

        float unit;
        int resultCount;
        Font font;
        override
        public void SetUp()
        {
            #region***初始化设置***
            myCal = new Calculator(@"../Configuration/configXA_C.xml");    
            unit = myCal.Unit;
            resultCount = myCal.ResultCount;
            #endregion


            myCal.runGRB(myCal.Mode);
            myCal.ResponseExportCSV();//导出表格

            #region 显示设置
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
            //cam.DrawSystem(this, 200);//画网格

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
            foreach (IPoint poi in entrs)
            {
                if (entrs.IndexOf(poi) <3)
                {
                    PushMatrix();
                //TextSize(unit);
                Translate(unit * poi.p, unit * poi.q);
                TextAlign(0, 1);
                    TextSize(unit);
        
                   Text("P" + entrs.IndexOf(poi).ToString(), 0, -40, 0);
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
            #endregion
            
            #region 绘制分区
            StrokeWeight(2f);
            int index1 = 0;//colour
            int index2 = 0;


            foreach (ZoneVar dv in myCal.ZoneVars)
            {
                index1 = myCal.Zones.IndexOf(dv.Zone);
                Fill((25 +23  * index1)%255,70, 245);
                //Fill(colorList[index1,0], colorList[index1, 1], colorList[index1, 2]);
                //Stroke(50+17* index, 220, 150);
                Stroke(255);
                var rect = dv.rectResults[resultN];
                BeginShape();
                Vertex(unit * rect.X1, unit * rect.Y1, 0);
                Vertex(unit * rect.X2, unit * rect.Y1, 0);
                Vertex(unit * rect.X2, unit * rect.Y2, 0);    
                Vertex(unit * rect.X1, unit * rect.Y2, 0);  
                EndShape();

                //Fill(150,150,150);
                Fill((25 + 23 * index1) % 255, 220, 145);
                string index = index1 + "-" + index2;
                if (index2 == 0)
                    index = index1.ToString();
                TextSize(1f * unit);
                TextAlign(1, 1);

                
                if (info)
                {
                    double buildingArea = dv.Area(resultN,unit) * dv.Zone.Building_area / dv.Zone.Result_area(resultN);//
                    double siteArea = Math.Round(dv.Area(resultN,unit), 0);
                    buildingArea = Math.Round(buildingArea, 0);
                    double temp = Math.Round(siteArea, 1);
                    Text(temp.ToString(), unit * rect.Center.p, unit * (rect.Center.q ), 0);
                }
                else
                {
                    Text(index, unit * rect.Center.p, unit * rect.Center.q, 0);//文字
                }
            }
            //建筑信息
            if (build)
            {

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

                    for (int i = 0; i < d.Count; i++)
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
            }
            #endregion

            #region 呈现结构
            //绘制中心区
            if (myCal.CoreVar != null)
            {
                NoFill();
                Stroke(150,150,150);
                StrokeWeight((float)myCal.Core.Stroke);
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
                foreach (Axis r in myCal.Axes)
                {
                    StrokeWeight((float)r.Width);
                    Line(unit * r.X1, unit * r.Y1, 0, unit * r.X2, unit * r.Y2, 0);
                }
            }
            
            if (myCal.GridVars!=null)
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
            //NoFill();
            //StrokeWeight(2);
            //Stroke(0);
            //BeginShape();
            //foreach (IPoint p in boundry)
            //{
            //    Vertex(unit * p.p, unit * p.q, 0);
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
            //Text("占地" + myCal.Site.Area() * unit * unit, 0, 3 * unit, 0);
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
