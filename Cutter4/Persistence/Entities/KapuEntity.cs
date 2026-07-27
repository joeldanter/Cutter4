using Cutter4.Domain;
using System.Collections.Generic;

namespace Cutter4.Persistence.Entities;
public class KapuEntity
{
    public long Id { get; set; }
    public int Length { get; set; }
    public int Height { get; set; }
    public string SerialNumber { get; set; }
    public PanelColors Color { get; set; }
    public long OrderId { get; set; }
    public OrderEntity Order { get; set; }
    public ICollection<ConfigurationItemEntity> ConfigurationItems { get; set; } = [];

    public static KapuEntity FromDomain(Kapu kapu)
        => new KapuEntity()
        {
            Length = kapu.Length,
            Height = kapu.Height,
            SerialNumber = kapu.SerialNumber,
            Color = kapu.Color
        };

    public Kapu ToDomain()
        => new Kapu(Length, Height, Color, SerialNumber);
}
