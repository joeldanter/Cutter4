using Cutter4.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace Cutter4.Persistence.Entities;

public class StorageEditEntity
{
    public long Id { get; set; }
    public DateTime Date { get; set; }
    public string Message { get; set; }
    public ICollection<StorageEditItemEntity> StorageEditItems { get; set; } = [];

    public StorageEdit ToDomain()
        => new StorageEdit(Date, Message, StorageEditItems
                .OrderBy(x => x.Position)
                .ToDictionary(x => x.Panel.ToKey(), x => x.DeltaCount));
}
