﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using DotNetCore.CAP;
using Elastic.Apm.AspNetCore;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.NetCoreAll;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Logging;
using TT.Abp.Mall.Liseners;
using TT.RabbitMQ;

namespace TT.SoMall
{
    public class Startup
    {
        public IConfiguration Configuration { get; set; }


        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            var configuration = services.GetConfiguration();

            services.AddCap(x =>
            {
                var rabbitOptions = configuration.GetSection("RabbitMQ");
                //配置数据库连接
                x.UseSqlServer(configuration.GetConnectionString("Default"));
                x.UseDashboard();

                //配置消息队列RabbitMQ
                x.UseRabbitMQ(option =>
                {
                    option.HostName = rabbitOptions["HostName"];
                    option.UserName = rabbitOptions["UserName"];
                    option.Password = rabbitOptions["Password"];
                });
            });

            services.AddSignalR();

            services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMQ"));
            services.AddSingleton<RabbitMqPublisher>();
            services.AddHostedService<PayOrderLisener>();
            // ABP
            services.AddApplication<SoMallHttpApiHostModule>();
            // ABP End
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseElasticApm(Configuration,
                new HttpDiagnosticsSubscriber(), /* Enable tracing of outgoing HTTP requests */
                new EfCoreDiagnosticsSubscriber()); /* Enable tracing of database calls through EF Core*/


            IdentityModelEventSource.ShowPII = true;

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.InitializeApplication();

            app.UseCapDashboard();

            app.UseEndpoints(endpoints => { endpoints.MapHub<GroupChatHub>("/groupchat"); });

            app.MapWhen(
                ctx =>
                    ctx.Request.Path.ToString().StartsWith("/Home/"),
                app2 =>
                {
                    app2.UseRouting();
                    app2.UseConfiguredEndpoints();
                }
            );
        }
    }
}