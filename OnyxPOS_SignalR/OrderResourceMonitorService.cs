using Microsoft.AspNetCore.SignalR;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base.Enums;
using TableDependency.SqlClient.Base.EventArgs;
using ErrorEventArgs = TableDependency.SqlClient.Base.EventArgs.ErrorEventArgs;

namespace OnyxPOS_SignalR
{
    public class OrderResourceMonitorService : BackgroundService
    {
        private readonly IHubContext<OrderHub> _hubContext;

        private readonly IConfiguration _configuration;

        private SqlTableDependency<OrderResource> _tableDependency;

        public OrderResourceMonitorService(
            IHubContext<OrderHub> hubContext,
            IConfiguration configuration)
        {
            _hubContext = hubContext;
            _configuration = configuration;
        }

        protected override Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            string connectionString =
                _configuration.GetConnectionString("DefaultConnection");

            _tableDependency =
                new SqlTableDependency<OrderResource>(
                    connectionString,
                    tableName: "OrderResource");

            _tableDependency.OnChanged += TableDependency_OnChanged;

            _tableDependency.OnError += TableDependency_OnError;

            _tableDependency.Start();

            return Task.CompletedTask;
        }

        private async void TableDependency_OnChanged(
            object sender,
            RecordChangedEventArgs<OrderResource> e)
        {
            var entity = e.Entity;

            // INSERT
            if (e.ChangeType == ChangeType.Insert)
            {
                await _hubContext.Clients.All.SendAsync(
                    "TableOccupied",
                    new
                    {
                        entity.Id,
                        entity.ResourceId,
                        entity.CustomerId,
                        entity.LocationId,
                        entity.ResourceTypeId,
                        Status = "Occupied"
                    });
            }

            // UPDATE
            if (e.ChangeType == ChangeType.Update)
            {
                if (entity.CheckoutOn != null)
                {
                    await _hubContext.Clients.All.SendAsync(
                        "TableReleased",
                        new
                        {
                            entity.Id,
                            entity.ResourceId,
                            entity.LocationId,
                            entity.ResourceTypeId,
                            Status = "Available"
                        });
                }
            }
        }

        private void TableDependency_OnError(
            object sender,
            ErrorEventArgs e)
        {
            Console.WriteLine(e.Error.Message);
        }

        public override void Dispose()
        {
            _tableDependency?.Stop();

            base.Dispose();
        }
    }
}
