using System;
using System.Collections.Generic;
using System.Text;

namespace InitialArrange
{
    public class Domain
    {
        public double min;
        public double max;

        public Domain(double min,double max)
        {
            if (min <= max)
            {
                this.min = min;
                this.max = max;
            }
            else
            {
                this.min = max;
                this.max = min;
            }
            //初次设定时按顺序排列
        }
        public Domain(double[] value)
        {
            var min = value[0];
            var max = value[1];
            if (min <= max)
            {
                this.min = min;
                this.max = max;
            }
            else
            {
                this.min = max;
                this.max = min;
            }
            //初次设定时按顺序排列
        }
        public double Deta { get => Math.Abs(max - min); }

    }
}
