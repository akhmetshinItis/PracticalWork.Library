using Asp.Versioning;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PracticalWork.Library.Cache.Redis;
using PracticalWork.Library.Data.Minio;
using PracticalWork.Library.Data.Reports.PostgreSql;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.MessageBroker;
using PracticalWork.Library.Services;
using PracticalWork.Reports.Web.Configuration;
using System.Text.Json.Serialization;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Reports.Web.Controllers;

namespace PracticalWork.Reports.Web;

public class Startup
{
    private static string _basePath = string.Empty;
    private IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;

        var globalPrefix = Configuration["GlobalPrefix"];
        _basePath = string.IsNullOrWhiteSpace(globalPrefix) ? string.Empty : $"/{globalPrefix.Trim('/')}";
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddReportsPostgreSqlStorage(cfg =>
        {
            var connectionString = Configuration
                .GetSection("App")
                .GetConnectionString(nameof(ReportsDbContext));
            var npgsqlDataSource = new NpgsqlDataSourceBuilder(connectionString)
                .EnableDynamicJson()
                .Build();

            cfg.UseNpgsql(npgsqlDataSource);
        });

        var mvcBuilder = services.AddControllers(opt =>
            {
                opt.Filters.Add<DomainExceptionFilter<AppException>>();
            });

        mvcBuilder.PartManager.ApplicationParts.Clear();
        mvcBuilder.PartManager.ApplicationParts.Add(
            new AssemblyPart(typeof(ReportController).Assembly));

        mvcBuilder
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        services.AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1.0);
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        services.AddSwaggerGen(c =>
        {
            c.UseOneOfForPolymorphism();
            c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "PracticalWork.Library.Contracts.xml"));
            c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "PracticalWork.Library.Controllers.xml"));
        });

        services.AddMessageBroker(Configuration).AddProducers();
        services.AddCache(Configuration);
        services.AddMinioFileStorage(Configuration);

        services.AddScoped<IReportService, ReportService>();
    }

    [UsedImplicitly]
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime,
        ILogger logger, IServiceProvider serviceProvider)
    {
        app.UsePathBase(new PathString(_basePath));

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                var descriptions = endpoints.DescribeApiVersions();
                foreach (var description in descriptions)
                {
                    var url = $"/swagger/{description.GroupName}/swagger.json";
                    var name = description.GroupName.ToUpperInvariant();
                    options.SwaggerEndpoint(url, name);
                }
            });
            endpoints.MapControllers();
        });
    }
}
