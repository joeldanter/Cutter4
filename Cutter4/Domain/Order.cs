using System;
using System.Collections.Generic;

namespace Cutter4.Domain
{
    public class Order
    {
        public DateTime Date { get; init; } = DateTime.Now;
        public List<Kapu> Kapuk { get; init; } = [];
        public List<Configuration> Configurations { get; init; } = [];
    }
}
