using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricRubbish
{
    public static class ElectricRubbishExtnum
    {
        public static AbstractPhysicalObject.AbstractObjectType ElectricRubbishAbstract;

        public static void RegisterValues()
        {
            ElectricRubbishAbstract = new AbstractPhysicalObject.AbstractObjectType("ElectricRubbishAbstract", true);
        }

        public static void UnregisterValues()
        {
            if (ElectricRubbishAbstract != null) { ElectricRubbishAbstract.Unregister(); ElectricRubbishAbstract = null; }
        }
    }
}
