using Cutter4.Model.Cutting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Model.DataBase
{
    public class Order
    {
        public DateTime DateTime { get; }
        public List<Kapu> Kapuk { get; }
        public PanelCollection<Configuration> Configurations { get; }

        public Order(DateTime dateTime, List<Kapu> kapuk, PanelCollection<Configuration> configurations)
        {
            DateTime = dateTime;
            Kapuk = kapuk;
            Configurations = configurations;
        }
    }
}
