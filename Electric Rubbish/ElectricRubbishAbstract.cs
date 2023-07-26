using ElectricRubbish;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricRubbish
{
    public class ElectricRubbishAbstract : AbstractPhysicalObject
    {
        public int electricCharge = 2;
        public ElectricRubbishAbstract(World world, WorldCoordinate pos, EntityID ID, int electricCharge) : base(world, ElectricRubbishExtnum.ElectricRubbishAbstract, null, pos, ID)
        {
            this.electricCharge = electricCharge;
        }
        public override void Realize()
        {
            base.Realize();
            if (realizedObject == null)
                realizedObject = new ElectricRubbish(this, world);
        }
        public override string ToString()
        {
            return base.ToString() + $"<oA>{electricCharge}";
        }
    }
}
