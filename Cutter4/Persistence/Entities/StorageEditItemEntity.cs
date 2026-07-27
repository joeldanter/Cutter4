using Cutter4.Domain;
using System.Windows.Media.Media3D;

namespace Cutter4.Persistence.Entities;

public partial class StorageEditItemEntity
{
    public long Id { get; set; }
    public long PanelId { get; set; }
    public int DeltaCount { get; set; }
    public long EditId { get; set; }
    public StorageEditEntity Edit { get; set; }
    public PanelEntity Panel { get; set; }
}
 