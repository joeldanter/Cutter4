using CommunityToolkit.Mvvm.Messaging;
using Cutter4.Domain;
using Cutter4.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Persistence.Repositories
{
    public interface IOrderRepository
    {
        Task SaveNewOrderAsync(Order order);
        Task UndoLastOrderAsync();
        Task<IList<Order>> ListOrdersAsync();
    }
    
    public class OrderRepository : IOrderRepository
    {
        IDbContextFactory<CutterContext> contextFactory;
        IMessenger messenger;

        public OrderRepository(IDbContextFactory<CutterContext> contextFactory, IMessenger messenger)
        {
            this.contextFactory = contextFactory;
            this.messenger = messenger;
        }
        
        public async Task SaveNewOrderAsync(Order order)
        {
            Dictionary<PanelKey, int> changedPanels = PanelCountChanges(order.Configurations);
            Dictionary<PanelKey, PanelEntity> panels = await PanelHelper.GetOrCreatePanelsAsync(changedPanels.Keys.AsEnumerable(), contextFactory);
            
            foreach (KeyValuePair<PanelKey, int> entry in changedPanels)
            {
                PanelKey key = entry.Key;
                int delta = entry.Value;
                int available = panels[key].StorageCount;

                if (available + delta < 0)
                    throw OrderRepositoryErrors.OrderSave_InsufficientStock(key, available, -delta);
            }

            Dictionary<Kapu, KapuEntity> kapuk = order.Kapuk.ToDictionary(k => k, KapuEntity.FromDomain);
            OrderEntity orderEntity = new()
            {
                Date = order.Date,
                Kapuk = kapuk.Values.ToList(),
                Configurations = order.Configurations
                    .Select(c => new ConfigurationEntity()
                    {
                        ConfigurationItems = c.ConfigurationItems.Select(i => new ConfigurationItemEntity()
                        {
                            Kapu = kapuk[i.Key],
                            Count = i.Value
                        }).ToList(),
                        Panel = panels[c.PanelKey],
                    }).ToList()
            };

            using CutterContext context = contextFactory.CreateDbContext();

            context.Panels.AttachRange(panels.Values.AsEnumerable());

            using (IDbContextTransaction transaction = await context.Database.BeginTransactionAsync())
            {
                await context.Orders.AddAsync(orderEntity);

                foreach (KeyValuePair<PanelKey, int> entry in changedPanels)
                    panels[entry.Key].StorageCount += entry.Value;

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            messenger.Send(new OrdersChangedMessage());
            messenger.Send(new StorageChangedMessage());
        }

        public async Task UndoLastOrderAsync()
        {
            using CutterContext context = contextFactory.CreateDbContext();

            OrderEntity lastOrder = await context.Orders
                .OrderByDescending(o => o.Id)
                .Include(o => o.Configurations)
                    .ThenInclude(c => c.ConfigurationItems)
                        .ThenInclude(i => i.Kapu)
                .Include(o => o.Configurations)
                    .ThenInclude(c => c.Panel)
                .FirstOrDefaultAsync() ?? throw OrderRepositoryErrors.OrderUndo_NoOrder();

            Dictionary<PanelKey, int> changedPanels = PanelCountChanges(lastOrder.Configurations.Select(c => c.ToDomain()));
            Dictionary<PanelKey, PanelEntity> panels = await PanelHelper.GetOrCreatePanelsAsync(changedPanels.Keys.AsEnumerable(), contextFactory);

            foreach (KeyValuePair<PanelKey, int> entry in changedPanels)
            {
                PanelKey key = entry.Key;
                int delta = entry.Value;
                int available = panels[key].StorageCount;

                if (available - delta < 0)
                    throw OrderRepositoryErrors.OrderUndo_InsufficientStock(key, available, delta);
            }

            context.ChangeTracker.Clear();
            context.Panels.AttachRange(panels.Values.AsEnumerable());

            using (IDbContextTransaction transaction = await context.Database.BeginTransactionAsync())
            {
                foreach (KeyValuePair<PanelKey, int> entry in changedPanels)
                    panels[entry.Key].StorageCount -= entry.Value;
                
                await context.Orders
                    .Where(o => o.Id == lastOrder.Id)
                    .ExecuteDeleteAsync();

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            messenger.Send(new OrdersChangedMessage());
            messenger.Send(new StorageChangedMessage());
        }

        static Dictionary<PanelKey, int> PanelCountChanges(IEnumerable<Configuration> configurations)
        {
            Dictionary<PanelKey, int> changes = configurations
                .GroupBy(c => new PanelKey(c.PanelKey.Color, c.PanelKey.Width, c.Capacity))
                .ToDictionary(g => g.Key, g => -g.Count());

            Dictionary<PanelKey, int> waste = configurations
                .GroupBy(c => (c.PanelKey.Color, c.PanelKey.Width, c.Capacity - c.ConfigurationItems.Sum(x => x.Key.Length * x.Value)))
                .Where(g => g.Key.Item3 != 0)
                .ToDictionary(g => new PanelKey(g.Key.Color, g.Key.Width, g.Key.Item3), g => g.Count());

            foreach (var (panel, count) in waste)
            {
                changes[panel] = changes.GetValueOrDefault(panel) + count;
            }

            return changes;
        }

        public async Task<IList<Order>> ListOrdersAsync()
        {
            using CutterContext context = contextFactory.CreateDbContext();
            List<OrderEntity> orderEntities = await context.Orders
                .Include(o => o.Kapuk)
                .Include(o => o.Configurations)
                    .ThenInclude(c => c.ConfigurationItems)
                .Include(o => o.Configurations)
                    .ThenInclude(c => c.Panel)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return [.. orderEntities.Select(o => o.ToDomain())];
        }
    }
}
