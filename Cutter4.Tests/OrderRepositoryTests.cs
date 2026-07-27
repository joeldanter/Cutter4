using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Cutter4.Domain.Cutting;
using Cutter4.Domain;
using Cutter4.Persistence.Repositories;
using System.Collections.ObjectModel;

namespace Cutter4.Tests
{
    public class OrderRepositoryTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
    {
        DatabaseFixture fixture;
        IOrderRepository orderRepository;
        IStorageRepository storageRepository;

        public OrderRepositoryTests(DatabaseFixture fixture)
        {
            this.fixture = fixture;
            orderRepository = new OrderRepository(fixture.ContextFactory, new WeakReferenceMessenger());
            storageRepository = new StorageRepository(fixture.ContextFactory, new WeakReferenceMessenger());
        }

        public async Task InitializeAsync()
        {
            await fixture.ResetDatabaseAsync();
        }
        
        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task EmptyOrders()
        {
            IList<Order> orders = await orderRepository.ListOrdersAsync();
        
            Assert.Empty(orders);
        }

        static IEnumerable<CutConfiguration> ConfigurationData()
        {
            yield return new (13200, []);
            yield return new (13200, [ 13200 ]);
            yield return new (13200, [ 13000, 200]);
            yield return new (13200, [ 3000 ]);
            yield return new (1000, []);
            yield return new (1000, [ 1000 ]);
            yield return new (4000, [ 1000, 500, 2000, 500 ]);
            yield return new (10200, [1000, 1000]);
            yield return new (8200, [1000]);
            yield return new (13200, [ 3000, 1000, 500, 8700 ]);
            yield return new (13200, [ 3000, 1000, 500, 800 ]);
            yield return new (13200, [ 1000, 1000, 1000, 200, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000 ]);
            yield return new (13200, [ 1000, 1000, 1000, 1000, 1000, 1000, 500, 1000, 1000, 1000, 1000, 1000, 1000 ]);
            yield return new (13200, [ 1000, 1000, 1000, 1000, 1000, 1000, 500, 1000, 1000, 1000, 1000, 1000, 1000 ]);
            yield return new (13200, [5000]);
            yield return new (13200, [6000]);
        }

        static Dictionary<int, int> StorageData()
        {
             return new()
             {
                { 13200, 15 },
                { 1000, 2 },
                { 4000, 2 },
                { 8200, 1 },
                { 10200, 1 }
             };
        }

        Order ConstructTestOrder(IList<CutConfiguration> configurationData, PanelColors color, PanelWidths width)
        {
            List<Kapu> kapuk = [];
            List<Configuration> confs = [];
            foreach(CutConfiguration cutConf in configurationData)
            {
                Configuration conf = new(
                    new PanelKey(color, width, cutConf.Capacity),
                    cutConf.Items.ToDictionary(length => new Kapu(length, 1000, color, TestIdGenerator.NextId().ToString()), _ => 1));
                kapuk.AddRange(conf.ConfigurationItems.Keys.AsEnumerable());
                confs.Add(conf);
            }

            Order order = new()
            {
                Date = DateTime.Now,
                Kapuk = kapuk,
                Configurations = confs
            };
            return order;
        }

        StorageEdit ConstructTestStorageEdit(Dictionary<int, int> storageData, PanelColors color, PanelWidths width)
        {
            StorageEdit edit = new(DateTime.Now, "meh", storageData.ToDictionary(x => new PanelKey(color, width, x.Key), x => x.Value));
            
            return edit;
        }

        [Fact]
        public async Task AddUndoOrder()
        {
            PanelColors color = PanelColors.F100;
            PanelWidths width = PanelWidths.Width610mm;
            int rep = 10;

            var editData = StorageData();
            List<CutConfiguration> confData = [.. ConfigurationData()];

            StorageEdit edit = ConstructTestStorageEdit(editData, color, width);
            for (int _ = 0; _ < rep; _++)
                await storageRepository.EditStorageAsync(edit);

            IDictionary<PanelKey, int> startStorage = await storageRepository.StoragePanelsAsync();

            for (int _ = 0; _ < rep; _++)
            {
                Order order = ConstructTestOrder(confData, color, width);
                await orderRepository.SaveNewOrderAsync(order);
            }
            
            for (int _ = 0; _ < rep; _++)
                await orderRepository.UndoLastOrderAsync();

            IDictionary<PanelKey, int> storage = await storageRepository.StoragePanelsAsync();
            IList<Order> orders = await orderRepository.ListOrdersAsync();

            Assert.Empty(orders);
            Assert.Equal(startStorage.Count, storage.Count);
            storage.Should().BeEquivalentTo(startStorage);
        }
    }
}
