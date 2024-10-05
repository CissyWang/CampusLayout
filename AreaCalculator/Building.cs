using System;
using System.Collections.Generic;
using System.Text;

namespace IndexCalculate
{
    public class Building
    {
        string name;
        string zone_name;
        int layer;
        double density;
        double area;
        int index;

        internal Building(int index,string name, int layer, double area, double density,string zone)
        {
            this.index = index;
            this.name = name;
            this.layer = layer;
            this.area = area;
            this.density = density;
            this.zone_name = zone;
        }

        public string Name { get => name;}
        public int Layer { get => layer;}
        //public double Area_per { get => area_per;}
        public double Density { get => density; }
        public double Area { get => area; set => area = value; }
        public double Site_area { get => area / layer / density; }
        public string Zone_name { get => zone_name; set => zone_name = value; }
        public double Floor_area { get =>  area / layer; }
        public int Index { get => index; set => index = value; }
    }
}
