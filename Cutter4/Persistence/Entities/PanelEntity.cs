using Cutter4.Domain;
using System.Collections.Generic;
using System.Windows.Navigation;

namespace Cutter4.Persistence.Entities;

public partial class PanelEntity
{
    public long Id { get; set; }
    public int Length { get; set; }
    public PanelColors Color { get; set; }
    public PanelWidths Width { get; set; }
    public int StorageCount { get; set; }
    public ICollection<ConfigurationEntity> Configurations { get; set; } = [];
    public ICollection<StorageEditItemEntity> StorageEditItems { get; set; } = [];

    public PanelKey ToKey()
        => new PanelKey(Color, Width, Length);
}
