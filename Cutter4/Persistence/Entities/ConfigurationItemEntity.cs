using Cutter4.Domain;

namespace Cutter4.Persistence.Entities;

public partial class ConfigurationItemEntity
{
    public long Id { get; set; }
    public long KapuId { get; set; }
    public long ConfigurationId { get; set; }
    public int Count { get; set; }
    public long OrderId { get; set; }
    public PanelColors Color { get; set; }
    public ConfigurationEntity Configuration { get; set; }
    public KapuEntity Kapu { get; set; }
}
