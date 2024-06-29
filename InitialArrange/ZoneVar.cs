using Gurobi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IndexCalculate;
using System.IO;

namespace InitialArrange
{
    public class ZoneVar
    {
        public List<IRectangle> rectResults = new List<IRectangle>();//导出多个的结果
        List<double> area = new List<double>();
        internal GRBVar x;
        internal GRBVar y;
        internal GRBVar dx;
        internal GRBVar dy;
        
        IZone zone;
        Site site;
        int isInteger;
        internal double lenToWidth=3;
        int index;
        Domain[] domain;

        private List<int[]> roadSide = new List<int[]>();
        private List<int> sigleP_link = new List<int>();
        private List<int> road_link = new List<int>();
        private List<int> road_align = new List<int>();
        private double align_tolerance;

        private Domain area_lim;

        public IZone Zone { get => zone; }
  

        #region Constructor
        public ZoneVar(IZone zone, Site site, int isInteger)
        {
            this.zone = zone;
            this.site = site;
            this.isInteger = isInteger;
            Area_lim = new Domain(0, site.W * site.H);
        }
        
        //用于Group\Core
        public ZoneVar(Site site, int isInteger, double lenToWidth)
        {
            this.site = site;
            this.isInteger = isInteger;
            this.lenToWidth = lenToWidth;
            Area_lim = new Domain(0, site.W * site.H);
        }
        
        public ZoneVar( Site site,Domain[] domain, int isInteger, double lenToWidth)
         {
            this.site = site;
            this.isInteger = isInteger;
            this.lenToWidth = lenToWidth;
            this.domain = domain;
            Area_lim = new Domain(0, site.W * site.H);

        }
        public void SetVar(GRBModel model)
        {
            x = model.AddVar(site.Xmin, site.Xmax, site.Xmin, GRB.CONTINUOUS, "x");
            y = model.AddVar(site.Ymin, site.Ymax, site.Ymin, GRB.CONTINUOUS, "y");
            if (isInteger == 1)//长宽变量为整数
            {
                dx = model.AddVar(1, site.W, 0, GRB.INTEGER, "dx");
                dy = model.AddVar(1, site.H, 0, GRB.INTEGER, "dy");
            }
            else//长宽变量为连续值
            {
                dx = model.AddVar(1, site.W, 0, GRB.CONTINUOUS, "dx");
                dy = model.AddVar(1, site.H, 0, GRB.CONTINUOUS, "dy");
            }

            model.AddConstr(x + dx / 2 <= site.Xmax, "x+dx/2<=xmax");
            model.AddConstr(y + dy / 2 <= site.Ymax, "y+dy/2<=ymax");
            model.AddConstr(x - dx / 2 >= site.Xmin, "x-dx/2>xmin");
            model.AddConstr(y - dy / 2 >= site.Ymin, "y-dy/2>=ymin");
            
            //最小长宽
            if (zone!=null&&zone.Length_min > 0)
            {
                model.AddConstr(dx >= zone.Length_min, "");
                model.AddConstr(dy >= zone.Length_min, "");
            }
            //设置分区长宽比例(1:lenToWidth)
            model.AddConstr(lenToWidth * dy >= dx, "r*dy>=dx");
            model.AddConstr(lenToWidth * dx >= dy, "r*dx>=dy");

            //对子分区面积限制
            model.AddQConstr(dy * dx >= Area_lim.min, "");
            if (Area_lim.max > 0)
                model.AddQConstr(dy * dx <= Area_lim.max, "");

            //分区范围
            if (domain != null)
            {
                model.AddConstr(x <= domain[0].max, " ");
                model.AddConstr(x >= domain[0].min, " ");
                model.AddConstr(y >= domain[1].min, " ");
                model.AddConstr(y <= domain[1].max, " ");
                if (domain.Length == 4)
                {
                    model.AddConstr(dx <= domain[2].max, " ");
                    model.AddConstr(dx >= domain[2].min, " ");
                    model.AddConstr(dy >= domain[3].min, " ");
                    model.AddConstr(dy <= domain[3].max, " ");
                }
            }
        }

        public void SiteConstr(GRBModel model)
        {
            //Minus Rectangle
            if (site.Minus != null)
            {
                GRBVar[] bool_minus = new GRBVar[4];
                foreach (IRectangle rect in site.Minus)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        bool_minus[i] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "bool_minus" + i );
                    }
                    model.AddConstr(x + dx / 2 <= rect.X1 + (1 - bool_minus[ 0]) * site.W, "x+dx/2<=X1");//
                    model.AddConstr(y + dy / 2 <= rect.Y1 + (1 - bool_minus[ 1]) * site.H, "y+dy/2<=Y1");//
                    model.AddConstr(x - dx / 2 >= rect.X2 - (1 - bool_minus[2]) * site.W, "x-dx/2>=X2");//
                    model.AddConstr(y - dy / 2 >= rect.Y2 - (1 - bool_minus[3]) * site.H, "y-dy/2>=Y2");//
                    model.AddConstr(bool_minus[0] + bool_minus[ 1] + bool_minus[ 2] + bool_minus[3] >= 1, "1+2+3+4 >=1");

                }
            }

            //OutsidePounts
            if (site.OutOfSite != null)
            {
                GRBVar[] bool_points = new GRBVar[4];
                foreach (IPoint poi in site.OutOfSite)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        bool_points[i] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "bool_points" + i + index);
                    }
                    model.AddConstr(x - dx / 2 -0.5+(1-bool_points[0])*site.W>= poi.p, " ");
                    model.AddConstr(x +dx / 2 +0.5-(1 - bool_points[1]) * site.W <= poi.p, " ");
                    model.AddConstr(y - dy / 2 - 0.5+ (1 - bool_points[2]) * site.H >= poi.q, " ");
                    model.AddConstr(y + dy / 2 + 0.5- (1 - bool_points[ 3]) * site.H <= poi.q, " ");
                    model.AddConstr(bool_points[ 0] + bool_points[ 1] + bool_points[2] + bool_points[3] >= 1, "1+2+3+4 >=1");
                }
            }

            //Road inside
            if (site.Roads != null)
            {
                foreach (Road r in site.Roads)
                {    
                    if (Math.Abs(r.UpDown) == 1)
                        OutsideRoadConstr(model, r);
                    if (r.UpDown == 2)
                        InsectRoadConstr(model, r);
                }
            }
        }
       
        private void OutsideRoadConstr(GRBModel model,Road r )
        {
            GRBVar[] bool_Road;
            double big = site.W * site.H;
            double k = Math.Sqrt(r.A * r.A + r.B * r.B);
            double temp = k * r.Width / 2;   //考虑道路距离 
            bool_Road = new GRBVar[5];
                for (int i = 0; i < 5; i++)
                {
                    bool_Road[i] = model.AddVar(0, 1, 0, GRB.BINARY, " bool Road" + i);
                }
                //-1保留上方，1保留下方
                model.AddConstr(r.UpDown * (r.A * (x - dx / 2) + r.B * (y - dy / 2) + r.C) + (1 - bool_Road[0]) * big >= temp, " ");
                model.AddConstr(r.UpDown * (r.A * (x + dx / 2) + r.B * (y + dy / 2) + r.C) + (1 - bool_Road[0]) * big >= temp, " ");
                model.AddConstr(r.UpDown * (r.A * (x - dx / 2) + r.B * (y + dy / 2) + r.C) + (1 - bool_Road[0]) * big >= temp, " ");
                model.AddConstr(r.UpDown * (r.A * (x + dx / 2) + r.B * (y - dy / 2) + r.C) + (1 - bool_Road[0]) * big >= temp, " ");
                model.AddConstr(x - dx / 2 + (1 - bool_Road[1]) * big >= r.Xrange.max, " ");
                model.AddConstr(x + dx / 2 - (1 - bool_Road[2]) * big <= r.Xrange.min, " ");
                model.AddConstr(y + dy / 2 - (1 - bool_Road[3]) * big <= r.Yrange.min, " ");
                model.AddConstr(y - dy / 2 + (1 - bool_Road[4]) * big >= r.Yrange.max, " ");
                model.AddConstr(bool_Road[0] + bool_Road[1] + bool_Road[2] + bool_Road[3] + bool_Road[4] >= 1, " ");
                //model.AddConstr(bool_Road[0] == 1, " ");

        }
        private void InsectRoadConstr(GRBModel model, Road r)
        {
            double k = Math.Sqrt(r.A * r.A + r.B * r.B);
            double temp = k * r.Width / 2;   //考虑道路距离 
            double big = site.W * site.H;
            GRBVar[] bool_Road= new GRBVar[2];
            for (int i = 0; i < 2; i++)
            {
                bool_Road[i] = model.AddVar(0, 1, 0, GRB.BINARY, " bool Road" + i);
            }

            model.AddConstr(r.A * (x - dx / 2) + r.B * (y - dy / 2) + r.C + (1 - bool_Road[0]) * big >= temp, " ");
            model.AddConstr(r.A * (x + dx / 2) + r.B * (y + dy / 2) + r.C + (1 - bool_Road[0]) * big >= temp, " ");
            model.AddConstr(r.A * (x - dx / 2) + r.B * (y + dy / 2) + r.C + (1 - bool_Road[0]) * big >= temp, " ");
            model.AddConstr(r.A * (x + dx / 2) + r.B * (y - dy / 2) + r.C + (1 - bool_Road[0]) * big >= temp, " ");

            model.AddConstr(r.A * (x - dx / 2) + r.B * (y - dy / 2) + r.C - (1 - bool_Road[1]) * big <= -temp, " ");
            model.AddConstr(r.A * (x + dx / 2) + r.B * (y + dy / 2) + r.C - (1 - bool_Road[1]) * big <= -temp, " ");
            model.AddConstr(r.A * (x - dx / 2) + r.B * (y + dy / 2) + r.C - (1 - bool_Road[1]) * big <= -temp, " ");
            model.AddConstr(r.A * (x + dx / 2) + r.B * (y - dy / 2) + r.C - (1 - bool_Road[1]) * big <= -temp, " ");

            model.AddConstr(bool_Road[0] + bool_Road[1] == 1, " ");
            int roadIndex = site.Roads.IndexOf(r);
            try
            {
                foreach (int[] sides in RoadSide)
                {
                    if (sides.Contains(roadIndex))
                    {
                        model.AddConstr(bool_Road[sides[1]] == 1, " ");
                    }
                }
            }
            catch { }
        }
        #endregion

        ///*设置互不重叠*
        internal void ConstrOverlap(GRBModel model, List<ZoneVar> districtVars, double spacing)
        {
            int i = districtVars.IndexOf(this);
            int c = districtVars.Count;

            GRBVar[] bools = new GRBVar[4];

            for (int j = i + 1; j < c; j++)
            {
                for (int n = 0; n < 4; n++)
                {
                    bools[n] =  model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "BOOL" + i + j);
                }

                model.AddConstr(districtVars[i].x + districtVars[i].dx / 2 + spacing <=
                    districtVars[j].x - districtVars[j].dx / 2 + (1 - bools[ 0]) * site.W,
                    "x+ dx/2 < x1-dx1/2" + i + j);//
                model.AddConstr(districtVars[i].y + districtVars[i].dy / 2 + spacing <=
                    districtVars[j].y - districtVars[j].dy / 2 + (1 - bools[ 1]) * site.H,
                    "y+ dy/2 < y1-dy1/2" + i + j);//
                model.AddConstr(districtVars[j].x + districtVars[j].dx / 2 + spacing <=
                    districtVars[i].x - districtVars[i].dx / 2 + (1 - bools[2]) * site.W,
                    "x1+ dx1/2 < x-dx/2" + i + j);//
                model.AddConstr(districtVars[j].y + districtVars[j].dy / 2 + spacing <=
                    districtVars[i].y - districtVars[i].dy / 2 + (1 - bools[3]) * site.H,
                    "y1 + dy1/2 < y-dy/2" + i + j);//
                model.AddConstr(bools[0] + bools[1] + bools[2] + bools[ 3] >= 1, "bool" + i + j);


            }
        }

        #region 控制位置
        // 到点的距离
        internal void SetEntranceObj(GRBQuadExpr expr, double k)
        {
            double distMin, distMax, deta;
            foreach (int i in SingleP_link)
            {
                distMin = site.Entrances[i].distToPoints_min(site.All_points);
                distMax = site.Entrances[i].distToPoints_max(site.All_points);
                deta = SingleP_link.Count * (distMax - distMin) / k;//(distMax - dist )/deta

                expr.AddConstant(-distMin / deta);
                expr.Add(1 / deta * (x - site.Entrances[i].p) * (x - site.Entrances[i].p));
                expr.Add(1 / deta * (y - site.Entrances[i].q) * (y - site.Entrances[i].q));
            }

        }

        //到道路的距离
        internal void SetRoadObj(GRBQuadExpr expr, double k)
        {
            foreach (int i in Road_link)
            {
                double a = site.Roads[i].A;
                double b = site.Roads[i].B;
                double c = site.Roads[i].C;
                double t = 1 / (a * a + b * b);
                double t1 = 100 / site.lineDistMax(site.Roads[i]);
                expr.Add(t * (a * x + b * y + c) * (a * x + b * y + c)*t1); ;
            }
        }

        //紧邻道路
        internal void SetRoadConstr(GRBModel model)
        {
            GRBVar[] bool_align = new GRBVar[4];
            foreach (int i in Road_align)
            {
                
                for (int j = 0; j < 4; j++)
                {
                    bool_align[j] = model.AddVar(0, 1, 0, GRB.BINARY, " ");
                }
                double width = site.Roads[i].Width;
                double a = site.Roads[i].A;
                double b = site.Roads[i].B;
                double c = site.Roads[i].C;
                double t = Math.Sqrt(a * a + b * b)*(align_tolerance+width/2);
                model.AddConstr((a * (x + dx / 2) + b * (y + dy / 2) + c) + (1 - bool_align[0]) * site.w >= - t, "");
                model.AddConstr((a * (x + dx / 2) + b * (y + dy / 2) + c) - (1 - bool_align[0]) * site.w <=  t, "");

                model.AddConstr((a * (x - dx / 2) + b * (y + dy / 2) + c) + (1 - bool_align[1]) * site.w >= - t, "");
                model.AddConstr((a * (x - dx / 2) + b * (y + dy / 2) + c) - (1 - bool_align[1]) * site.w <=  t, "");

                model.AddConstr((a * (x + dx / 2) + b * (y - dy / 2) + c) + (1 - bool_align[2]) * site.w >= - t, "");
                model.AddConstr((a * (x + dx / 2) + b * (y - dy / 2) + c) - (1 - bool_align[2]) * site.w <=  t, "");

                model.AddConstr((a * (x - dx / 2) + b * (y - dy / 2) + c) + (1 - bool_align[3]) * site.w >= -t, "");
                model.AddConstr((a * (x - dx / 2) + b * (y - dy / 2) + c) - (1 - bool_align[3]) * site.w <=  t, "");

                model.AddConstr(bool_align[0] + bool_align[1] + bool_align[2] + bool_align[3] >= 1, " ");

            }
        }

        //虚轴。邻
        internal void LineAlign(GRBModel model, Line line, double width)
        {
            GRBVar[] bool_align = new GRBVar[4];
            for (int j = 0; j < 4; j++)
            {
                bool_align[j] = model.AddVar(0, 1, 0, GRB.BINARY, " ");
            }

            double a = line.A;
            double b = line.B;
            double c = line.C;
            double t = Math.Sqrt(a * a + b * b);

            if (b * a < 0 || b == 0)//k>0
            {
                model.AddConstr((a * (x - dx / 2) + b * (y + dy / 2) + c) + (1 - bool_align[0]) * site.w >= width / 2 * t, "");
                model.AddConstr((a * (x - dx / 2) + b * (y + dy / 2) + c) - (1 - bool_align[0]) * site.w <= width / 2 * t, "");
                model.AddConstr((a * (x + dx / 2) + b * (y - dy / 2) + c) - (1 - bool_align[1]) * site.w <= -width / 2 * t, "");
                model.AddConstr((a * (x + dx / 2) + b * (y - dy / 2) + c) + (1 - bool_align[1]) * site.w >= -width / 2 * t, "");
            }
            else//k<0
            {
                model.AddConstr((a * (x + dx / 2) + b * (y + dy / 2) + c) - (1 - bool_align[0]) * site.w <= width / 2 * t, "");
                model.AddConstr((a * (x + dx / 2) + b * (y + dy / 2) + c) + (1 - bool_align[0]) * site.w >= width / 2 * t, "");
                model.AddConstr((a * (x - dx / 2) + b * (y - dy / 2) + c) + (1 - bool_align[1]) * site.w >= -width / 2 * t, "");
                model.AddConstr((a * (x - dx / 2) + b * (y - dy / 2) + c) - (1 - bool_align[1]) * site.w <= -width / 2 * t, "");
            }
            model.AddConstr(bool_align[0] + bool_align[1] >= 1, " ");

            model.AddConstr(x - dx / 2 - (1 - bool_align[2]) <= line.Xrange.max, "");
            model.AddConstr(x + dx / 2+ (1 - bool_align[2]) >= line.Xrange.min, "");
            model.AddConstr(y - dy / 2 - (1 - bool_align[3]) <= line.Yrange.max, "");
            model.AddConstr(y + dy / 2 + (1 - bool_align[3]) >= line.Yrange.min, "");
            model.AddConstr(bool_align[2] + bool_align[3] >= 1, " ");
        }

        //实轴，在之间
        internal void LineAlign(GRBModel model, Line line, double buffer,bool centered)
        {
            GRBVar[] bool_align = new GRBVar[2];
            for (int j = 0; j < 2; j++)
            {
                bool_align[j] = model.AddVar(0, 1, 0, GRB.BINARY, " ");
            }

            double a = line.A;
            double b = line.B;
            double c = line.C;
            double t = Math.Sqrt(a * a + b * b);

            model.AddConstr((a * (x + dx / 2) + b * (y + dy / 2) + c) >= -buffer / 2 * t, "");
            model.AddConstr((a * (x + dx / 2) + b * (y + dy / 2) + c) <= buffer / 2 * t, "");

            model.AddConstr((a * (x - dx / 2) + b * (y + dy / 2) + c) >= -buffer / 2 * t, "");
            model.AddConstr((a * (x - dx / 2) + b * (y + dy / 2) + c) <= buffer / 2 * t, "");

            model.AddConstr((a * (x + dx / 2) + b * (y - dy / 2) + c) >= -buffer / 2 * t, "");
            model.AddConstr((a * (x + dx / 2) + b * (y - dy / 2) + c) <= buffer / 2 * t, "");

            model.AddConstr((a * (x - dx / 2) + b * (y - dy / 2) + c) >= -buffer / 2 * t, "");
            model.AddConstr((a * (x - dx / 2) + b * (y - dy / 2) + c) <= buffer / 2 * t, "");

            model.AddConstr(x - dx / 2 - (1 - bool_align[0]) <= line.Xrange.max, "");
            model.AddConstr(x + dx / 2 + (1 - bool_align[0]) >= line.Xrange.min, "");
            model.AddConstr(y - dy / 2 - (1 - bool_align[1]) <= line.Yrange.max, "");
            model.AddConstr(y + dy / 2 + (1 - bool_align[1]) >= line.Yrange.min, "");
            model.AddConstr(bool_align[0] + bool_align[1] >= 1, " ");

            if (centered)
                    model.AddConstr(a * x + b * y + c == 0, "");
            
        }

        //分区之间靠近
        internal void ZoneLinkObj(GRBQuadExpr obj,ZoneVar dv)
        {
            
            double t = 100 / (site.w * site.w + site.h * site.h);

            obj.Add(((x + dx / 2 - dv.x - dv.dx / 2) * (x + dx / 2 - dv.x - dv.dx / 2) +
                (y + dy / 2 - dv.y - dv.dy / 2) * (y + dy / 2 - dv.y - dv.dy / 2)) * t);

        }
        internal void ZoneDistObj(GRBQuadExpr obj, ZoneVar dv)
        {
            double t = 100 / (site.w * site.w + site.h * site.h);

            obj.Add(100-((x + dx / 2 - dv.x - dv.dx / 2) * (x + dx / 2 - dv.x - dv.dx / 2) +
                (y + dy / 2 - dv.y - dv.dy / 2) * (y + dy / 2 - dv.y - dv.dy / 2)) * t);

        }
        #endregion

        internal void SetResult(GRBModel model,int result)
        {
            float xr = (float)Math.Round(x.Xn,2);
            float yr = (float)Math.Round(y.Xn, 2);
            float dxr = (float)Math.Round(dx.Xn, 2);
            float dyr = (float)Math.Round(dy.Xn, 2);
            var rect = new IRectangle(new IPoint(xr, yr), dxr, dyr);//

            area.Add(rect.Area);
            //if (zone != null)
            //{
            //    Console.WriteLine(zone.name + "_results" + result + ":  (" + xr + "," + yr + ")" + "," + dxr + "," + dyr);
            //}
            rectResults.Add(rect);
        }

        internal void inOrOutOthers(GRBModel model,ZoneVar other,double stroke,InOrOut inside,bool align)
        {
            GRBVar[] inOut = new GRBVar[5];
            GRBVar[] isAlign;
            for (int j = 0; j < 5; j++)
            {
                inOut[j] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "bool_out" + j);
            }
            model.AddConstr( x +  dx / 2 - (1 - inOut[0]) * site.w <= other.x - other.dx / 2 -  stroke / 2, "");
            model.AddConstr( x -  dx / 2 + (1 - inOut[1]) * site.w >= other.x + other.dx / 2 +  stroke / 2, "");
            model.AddConstr( y +  dy / 2 - (1 - inOut[2]) * site.h <= other.y - other.dy / 2 -  stroke / 2, "");
            model.AddConstr( y -  dy / 2 + (1 - inOut[3]) * site.h >= other.y + other.dy / 2 +  stroke / 2, "");

            model.AddConstr( x +  dx / 2 - (1 - inOut[4]) * site.w <= other.x + other.dx / 2 -  stroke / 2, "");
            model.AddConstr( x -  dx / 2 + (1 - inOut[4]) * site.w >= other.x - other.dx / 2 +  stroke / 2, "");
            model.AddConstr( y +  dy / 2 - (1 - inOut[4]) * site.h <= other.y + other.dy / 2 -  stroke / 2, "");
            model.AddConstr( y -  dy / 2 + (1 - inOut[4]) * site.h >= other.y - other.dy / 2 +  stroke / 2, "");

            model.AddConstr(inOut[0] + inOut[1] + inOut[2] + inOut[3] + inOut[4] >= 1, "1+2+3+4>=1");

            //必须在内
            if (inside == InOrOut.Inside) { 
                model.AddConstr(inOut[4] == 1, "");
                if (align)
                {
                    isAlign = new GRBVar[4];
                    for (int j = 0; j < 4; j++)
                    {
                        isAlign[j] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "is_align" + j);
                    }
                    model.AddConstr(x + dx / 2 + (1 - isAlign[0]) * site.w >= other.x + other.dx / 2 - stroke / 2, "");
                    model.AddConstr(x - dx / 2 - (1 - isAlign[1]) * site.w <= other.x - other.dx / 2 + stroke / 2, "");
                    model.AddConstr(y + dy / 2 + (1 - isAlign[2]) * site.h >= other.y + other.dy / 2 - stroke / 2, "");
                    model.AddConstr(y - dy / 2 - (1 - isAlign[3]) * site.h <= other.y - other.dy / 2 + stroke / 2, "");
                    model.AddConstr(isAlign[0] + isAlign[1] + isAlign[2] + isAlign[3] >= 1, "1+2+3+4>=1");

                }
            }
            //必须在外
            else if (inside== InOrOut.Outside)
            {
                model.AddConstr(inOut[4] == 0, "");
                if (align)
                {
                    model.AddConstr(x + dx / 2 >= other.x - other.dx / 2 - stroke / 2, "");
                    model.AddConstr(x - dx / 2 <= other.x + other.dx / 2 + stroke / 2, "");
                    model.AddConstr(y + dy / 2 >= other.y - other.dy / 2 - stroke / 2, "");
                    model.AddConstr(y - dy / 2 <= other.y + other.dy / 2 +stroke / 2, "");
                }
            }
        }


        #region 封装
        internal Domain x_lim;
        internal Domain y_lim;
        internal Domain dx_lim;
        internal Domain dy_lim;
        internal Domain X_lim { set => x_lim = value; }
        internal Domain Y_lim { set => y_lim = value; }
        internal Domain Dx_lim { set => dx_lim = value; }
        internal Domain Dy_lim { set => dy_lim = value; }
        public int Index
        {
            get
            {
                int index = 0;
                foreach (ZoneVar dv in Zone.ZoneVars)
                {
                    if (dv == this)
                        break;
                    else
                        index++;
                }
                return index;
            }
        }

        internal double Align_tolerance { get => align_tolerance; set => align_tolerance = value; }
        internal List<int> Road_align { get => road_align; set => road_align = value; }
        internal List<int[]> RoadSide { get => roadSide; set => roadSide = value; }
        internal List<int> SingleP_link { get => sigleP_link; set => sigleP_link = value; }
        internal List<int> Road_link { get => road_link; set => road_link = value; }
        internal Domain Area_lim { get => area_lim; set => area_lim = value; }

        public double Area(int resultN,float unit)
        {
            return unit*unit*area[resultN];
        }

        public double BuildingArea(int resultN)
        {
            double buildingArea=0;
            if (zone != null)
            {
                buildingArea = area[resultN] * zone.unit * zone.unit * Zone.Building_area / Zone.Result_area(resultN);
            }
            return buildingArea;
        }

        public double Ratio(int resultN)
        {
           return area[resultN] * zone.unit * zone.unit / Zone.Result_area(resultN);
        }

        public double floor_Area(int resultN)
        {
            return Ratio(resultN) * zone.Buildings.FloorArea_all();
        }  

        public List<double[]> BuildingInfo(int resultN)
        {
            List<double[]> info = new List<double[]>();
            foreach(Building b in zone.Buildings)
            {
                var i =new double[] { Math.Round(b.Floor_area * Ratio(resultN), 2), b.Layer };
                info.Add(i);
            }
            return info;
        }

        internal void WriteResults(StreamWriter sw,int resultCount)
        {
            string dataStr;

                dataStr = Zone.name;

            for (int n = 0; n < resultCount; n++)
            {
                try
                {
                    dataStr += $",({rectResults[n].X1};{rectResults[n].Y1};" +
                        $"{ rectResults[n].X2 };{rectResults[n].Y2});" +
                        $"{Math.Round(Area(n, zone.unit), 2)};{Math.Round(BuildingArea(n), 2)}";
                    if (BuildingArea(n) > 0&&zone.Buildings!=null)
                    {
                        foreach (double[] info in BuildingInfo(n))
                        {
                            dataStr += $";{info[0]};{info[1]}";
                        }
                    }
                }
                catch { return; }
            }
            sw.WriteLine(dataStr);
        }
        internal void WriteStructureResults(StreamWriter sw, int resultCount)
        {
            string dataStr;
            dataStr = "Rectangle";
            for (int n = 0; n < resultCount; n++)
            {
                try
                {
                    dataStr += $",({rectResults[n].X1};{rectResults[n].Y1};" +
                        $"{ rectResults[n].X2 };{rectResults[n].Y2})";

                }
                catch { return; }
            }
            sw.WriteLine(dataStr);
        }
        #endregion
    }
}
