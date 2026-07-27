using CommunityToolkit.Mvvm.Messaging;
using Cutter4.Domain;
using Cutter4.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cutter4.Persistence.Repositories
{
    public interface IStorageRepository
    {
        Task<IDictionary<PanelKey, int>> StoragePanelsAsync();
        Task EditStorageAsync(StorageEdit edit);
        Task<IList<StorageEdit>> ListStorageEditsAsync();
    }

    public class StorageRepository : IStorageRepository
    {
        IDbContextFactory<CutterContext> contextFactory;
        IMessenger messenger;

        public StorageRepository(IDbContextFactory<CutterContext> contextFactory, IMessenger messenger)
        {
            this.contextFactory = contextFactory;
            this.messenger = messenger;
        }

        public async Task<IDictionary<PanelKey, int>> StoragePanelsAsync()
        {
            using CutterContext context = contextFactory.CreateDbContext();
            return await context.Panels
                .Where(p => p.StorageCount > 0)
                .ToDictionaryAsync(p => new PanelKey(p.Color, p.Width, p.Length), p => p.StorageCount);
        }
         
        public async Task EditStorageAsync(StorageEdit edit)
        {
            IEnumerable<PanelKey> panelKeys = edit.StorageEditItems.Select(x => x.Key);
            Dictionary<PanelKey, PanelEntity> panels = await PanelHelper.GetOrCreatePanelsAsync(panelKeys, contextFactory);

            foreach(KeyValuePair<PanelKey, int> entry in edit.StorageEditItems)
            {
                PanelKey key = entry.Key;
                int available = panels[key].StorageCount;
                int delta = entry.Value;

                if (available + delta < 0)
                    throw StorageRepositoryErrors.StorageEdit_InsufficientStock(key, available, -delta);
            }

            List<StorageEditItemEntity> editItems = edit.StorageEditItems
                .Select(x => new StorageEditItemEntity()
                {
                    Panel = panels[x.Key],
                    DeltaCount = x.Value
                })
                .ToList();
            StorageEditEntity editEntity = new()
            {
                Date = edit.Date,
                Message = edit.Message,
                StorageEditItems = editItems
            };

            using CutterContext context = contextFactory.CreateDbContext();
            
            context.Panels.AttachRange(panels.Values.AsEnumerable());

            using (IDbContextTransaction transaction = await context.Database.BeginTransactionAsync())
            {
                await context.StorageEdits.AddAsync(editEntity);

                foreach (StorageEditItemEntity editItem in editEntity.StorageEditItems)
                {
                    editItem.Panel.StorageCount = editItem.Panel.StorageCount + editItem.DeltaCount;
                }

                await context.SaveChangesAsync();

                await transaction.CommitAsync();
            }

            messenger.Send(new StorageChangedMessage());
            messenger.Send(new EditsChangedMessage());
        }

        public async Task<IList<StorageEdit>> ListStorageEditsAsync()
        {
            using CutterContext context = contextFactory.CreateDbContext();
            return await context.StorageEdits
                .Include(e => e.StorageEditItems)
                    .ThenInclude(e => e.Panel)
                .OrderByDescending(e => e.Id)
                .Select(e => e.ToDomain())
                .ToListAsync();
        }
    }

    internal static class PanelHelper
    {
        public static async Task<Dictionary<PanelKey, PanelEntity>> GetOrCreatePanelsAsync(IEnumerable<PanelKey> panelKeys, IDbContextFactory<CutterContext> contextFactory)
        {
            using CutterContext context = contextFactory.CreateDbContext();
            using (IDbContextTransaction transaction = await context.Database.BeginTransactionAsync()) ;

            // this queries the whole table but idc
            Dictionary<PanelKey, PanelEntity> dbPanels = await context.Panels
            .ToDictionaryAsync(x => new PanelKey(x.Color, x.Width, x.Length));

            Dictionary<PanelKey, PanelEntity> result = [];
            foreach (PanelKey panelKey in panelKeys)
            {
                if (!result.ContainsKey(panelKey))
                {
                    if (!dbPanels.TryGetValue(panelKey, out PanelEntity? panel))
                    {
                        panel = new()
                        {
                            Color = panelKey.Color,
                            Width = panelKey.Width,
                            Length = panelKey.Length,
                            StorageCount = 0
                        };
                        await context.Panels.AddAsync(panel);
                    }

                    result[panelKey] = panel;
                }
            }

            await context.SaveChangesAsync();
            return result;
        }
    }
}
