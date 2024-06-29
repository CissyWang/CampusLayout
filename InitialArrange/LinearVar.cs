using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;

namespace InitialArrange
{
    public class LinearVar
    {
        int index;
        int direction;
        float stroke;
        Domain[] range;
        double unit;

        internal GRBVar road; //唯一变量
        List<Line> resultLine = new List<Line>();
        
        internal List<int> zonesLorB;
        internal List<int> zonesRorT;
        /// <summary>
        /// Directions: 0 is vertical and 1 is horizontal 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="Direction"></param>
        /// <param name="stroke"></param>
        /// <param name="range"></param>
        internal LinearVar(int index,int Direction, float stroke, Domain[] range,double unit)
        {
            this.index = index;
            this.direction = Direction;
            this.stroke = stroke;
            this.range = range;
            this.unit = unit;
        }
        public void SetVar(GRBModel model,Site site)
        {
            if (Direction == 0)
            {
                road = model.AddVar(site.Xmin, site.Xmax, 0, GRB.CONTINUOUS, "x");
                if (range[1].min < site.Ymin)
                    range[1].min = site.Ymin;
                if (range[1].max > site.Ymax)
                    range[1].max = site.Ymax;

            }
            else
            {
                road = model.AddVar(site.Ymin, site.Ymax, 0, GRB.CONTINUOUS, "y");
                if (range[1].min < site.Xmin)
                    range[1].min = site.Xmin;
                if (range[1].max > site.Xmax)
                    range[1].max = site.Xmax;
            }
            model.AddRange(road, range[0].min, range[0].max, " ");
        }
       public List<Line> ResultLine { get => resultLine;}
        public int Direction { get => direction;}
        public float Stroke { get => stroke; }

        internal void LineConstr(GRBModel model, List<ZoneVar> zoneVars,Site site)
        {

            int c = zoneVars.Count;
            GRBVar[] road_bools = new GRBVar[ 4];

            switch (Direction)
            {
                case 0:
                    //竖向道路
                    for (int d = 0; d < c; d++)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            road_bools[i] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, " ");
                        }
                        model.AddConstr(zoneVars[d].x - zoneVars[d].dx / 2 + (1 - road_bools[0]) * site.W
                            >= road + stroke/unit, "x-dx/2>=roadX ");//
                        model.AddConstr(zoneVars[d].x + zoneVars[d].dx / 2 - (1 - road_bools[1]) * site.W
                            <= road - stroke/unit, "x+dx/2<=roadX ");//

                        model.AddConstr(zoneVars[d].y + zoneVars[d].dy / 2 - (1 - road_bools[2]) * site.H
                            <= range[1].min, "  ");//
                        model.AddConstr(zoneVars[d].y - zoneVars[d].dy / 2 + (1 - road_bools[3]) * site.H
                            >= range[1].max, "  ");//
                        model.AddConstr(road_bools[ 0] + road_bools[1] + road_bools[2] + road_bools[3] == 1, " ");

                        if (zonesLorB != null && zonesLorB.Contains(zoneVars[d].Zone.Index))
                        {
                            model.AddConstr(road_bools[1] == 1, "");

                        }
                        if (zonesRorT != null && zonesRorT.Contains(zoneVars[d].Zone.Index))
                        {
                            model.AddConstr(road_bools[0] == 1, "");

                        }
                    }
                        break;
                case 1:
                    //横向道路
                    for (int d = 0; d < c; d++)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            road_bools[i] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, " ");
                        }
                        model.AddConstr(zoneVars[d].y - zoneVars[d].dy / 2 + (1 - road_bools[0]) * site.H >= road + stroke/unit, "y-dy/2>=roadY ");
                        model.AddConstr(zoneVars[d].y + zoneVars[d].dy / 2 - (1 - road_bools[1]) * site.H <= road - stroke/unit, "y+dy/2<=roadY ");
                        model.AddConstr(zoneVars[d].x + zoneVars[d].dx / 2 - (1 - road_bools[2]) * site.W <= range[1].min, "  ");//
                        model.AddConstr(zoneVars[d].x - zoneVars[d].dx / 2 + (1 - road_bools[3]) * site.W >= range[1].max, "  ");//

                        model.AddConstr(road_bools[0] + road_bools[1] + road_bools[2] + road_bools[3] == 1, " ");
                       
                        if (zonesLorB!=null&&zonesLorB.Contains(zoneVars[d].Zone.Index))
                        {
                            
                            model.AddConstr(road_bools[1] == 1, "");
                        }
                        if (zonesRorT != null && zonesRorT.Contains(zoneVars[d].Zone.Index))
                        {

                            model.AddConstr(road_bools[0] == 1, "");
                        }
                    }
                    break;
            }
        }

        internal void setResults(GRBModel model)
        {
            float roadLoc;
            roadLoc = (float)road.Xn;
            
            if (Direction == 0)
               resultLine.Add(new Line(roadLoc, (float)range[1].min, roadLoc, (float)range[1].max));
            else
                resultLine.Add(new Line((float)range[1].min, roadLoc, (float)range[1].max, roadLoc));
        }

        internal void WriteResults(StreamWriter sw, int resultCount)
        {
            string dataStr;
            dataStr = (stroke/unit).ToString();
            for (int n = 0; n < resultCount; n++)
            {
                try
                {
                    dataStr += $",({resultLine[n].X1};{resultLine[n].Y1};" +
                        $"{ resultLine[n].X2 };{resultLine[n].Y2})" ;
                }
                catch { return; }
            }
            sw.WriteLine(dataStr);
        }
    }
}
