using Microsoft.AspNetCore.SignalR;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base.EventArgs;
using ErrorEventArgs = TableDependency.SqlClient.Base.EventArgs.ErrorEventArgs;

namespace OnyxPOS_SignalR
{
    public class TierMonitorService : BackgroundService
    {
        private readonly IHubContext<OrderHub> _hubContext;

        private readonly IConfiguration _configuration;

        private SqlTableDependency<mstTier> _tableDependency;

        public TierMonitorService(
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
                _configuration.GetConnectionString(
                    "DefaultConnection");

            _tableDependency =
                new SqlTableDependency<mstTier>(
                    connectionString,
                    tableName: "mstTier");

            _tableDependency.OnChanged +=
                TableDependency_OnChanged;

            _tableDependency.OnError +=
                TableDependency_OnError;

            _tableDependency.Start();

            Console.WriteLine(
                "mstTier monitoring started");

            return Task.CompletedTask;
        }

        private async void TableDependency_OnChanged(
            object sender,
            RecordChangedEventArgs<mstTier> e)
        {
            var entity = e.Entity;

            Console.WriteLine(
                $"Tier Changed: {entity.Name}");

            // ONLY ACTIVE TIER
            if (entity.CurrentlyApplicable == true)
            {
                await _hubContext.Clients.All.SendAsync(
                    "TierActivated",
                    new
                    {
                        entity.Id,
                        entity.Name,
                        entity.ColorCode,
                        entity.CurrentlyApplicable
                    });

                Console.WriteLine(
                    $"ACTIVE TIER: {entity.Name}");
            }

            // TIER DEACTIVATED
            if (entity.CurrentlyApplicable == false)
            {
                await _hubContext.Clients.All.SendAsync(
                    "TierDeactivated",
                    new
                    {
                        entity.Id,
                        entity.Name
                    });

                Console.WriteLine(
                    $"DEACTIVE TIER: {entity.Name}");
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
