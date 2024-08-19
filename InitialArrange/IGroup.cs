using System.Collections.Generic;
//using Calculate_Area.BuildingList;
//功能分区类


namespace InitialArrange
{
    public class IGroup : ZoneBasic
    {

        internal Domain[] domain;
        internal double stroke;
        internal double minimizeWeight;
        internal List<int> insideZones;
        internal bool insideOnly;
        internal bool insideAlign;
        internal List<int> outsideZones;
        internal bool outsideAlign;
        internal ZoneVar zoneVar;

        public double Stroke { get => stroke; set => stroke = value; }

        public IGroup(int unit) : base(unit)
        {

        }

    }
}

