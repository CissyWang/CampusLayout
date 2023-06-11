using System;
using System.Collections.Generic;
using System.Text;

namespace IndexCalculate
{
    public class Building
    {
        string name;
        string district_name;
        int layer;
        //double area_per;
        double density;
        double area;
        double site_area;
        double floor_area;
        int index;

        internal Building(int index,string name, int layer, double area, double density,string district)
        {
            this.index = index;
            this.name = name;
            this.layer = layer;
            this.area = area;
            this.density = density;
            this.district_name = district;
        }

        public string Name { get => name;}
        public int Layer { get => layer;}
        //public double Area_per { get => area_per;}
        public double Density { get => density; }
        public double Area { get => area; set => area = value; }
        public double Site_area { get => this.site_area = area / layer / density; }
        public string District_name { get => district_name; set => district_name = value; }
        public double Floor_area { get => this.floor_area = area / layer; }
    }
}
