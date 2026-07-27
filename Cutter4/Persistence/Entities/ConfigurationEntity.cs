using Cutter4.Domain;
using System.Collections.Generic;
using System.Linq;

namespace Cutter4.Persistence.Entities;

public class ConfigurationEntity
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long PanelId { get; set; }
    public PanelColors Color { get; set; }
    public OrderEntity Order { get; set; }
    public PanelEntity Panel { get; set; }
    public ICollection<ConfigurationItemEntity> ConfigurationItems { get; set; } = [];

    public Configuration ToDomain()
        => new Configuration(Panel.ToKey(), ConfigurationItems.ToDictionary(x => x.Kapu.ToDomain(), x => x.Count));
}
