using Cutter4.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cutter4.Persistence.Entities;

public class OrderEntity
{
    public long Id { get; set; }
    public DateTime Date { get; set; }
    public ICollection<KapuEntity> Kapuk { get; set; } = [];
    public ICollection<ConfigurationEntity> Configurations { get; set; } = [];

    public Order ToDomain()
        => new Order()
        {
            Date = Date,
            Kapuk = [.. Kapuk.OrderBy(k => k.Position).Select(k => k.ToDomain())],
            Configurations = [.. Configurations.OrderBy(c => c.Position).Select(c => c.ToDomain())]
        };
}
