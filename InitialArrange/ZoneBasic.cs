//分区基础类，包括功能分区、组团、核等等

namespace InitialArrange
{
    public class ZoneBasic
    {
        internal int index;
        internal int unit;

        internal double site_area;
        internal double lenToWidth = 3;

        public ZoneBasic(int unit)
        {
            this.unit = unit;
        }
        public int Index { get => index; }

        public double Site_area { get => site_area; set => site_area = value; }
        public double S { get => site_area / unit / unit; }


    }
}

