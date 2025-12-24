using Microsoft.EntityFrameworkCore;
using Npgsql;
using PracticalWork.Library;
using PracticalWork.Library.Cache.Redis;
using PracticalWork.Library.Data.Minio;
using PracticalWork.Library.Data.PostgreSql;
using PracticalWork.Library.Data.Reports.PostgreSql;
using PracticalWork.Library.MessageBroker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMessageBroker(builder.Configuration)
    .AddConsumers()
    .AddProducers();
builder.Services.AddMinioFileStorage(builder.Configuration);
builder.Services.AddDomain();
builder.Services.AddCache(builder.Configuration);
builder.Services.AddPostgreSqlStorage(cfg =>
{
    var connectionString = builder.Configuration
        .GetSection("App")
        .GetConnectionString(nameof(AppDbContext));
    var npgsqlDataSource = new NpgsqlDataSourceBuilder(connectionString)
        .EnableDynamicJson()
        .Build();

    cfg.UseNpgsql(npgsqlDataSource);
});
builder.Services.AddReportsPostgreSqlStorage(cfg =>
    {
        var connectionString = builder.Configuration
            .GetSection("App")
            .GetConnectionString(nameof(ReportsDbContext));
        var npgsqlDataSource = new NpgsqlDataSourceBuilder(connectionString)
            .EnableDynamicJson()
            .Build();

        cfg.UseNpgsql(npgsqlDataSource);
    }    
);
var host = builder.Build();
host.Run();