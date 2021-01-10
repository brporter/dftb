using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using dftbsvc.Services;
using dftbsvc.Models;

namespace dftbsvc
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "dftbsvc", Version = "v1" });
            });

            services.AddSingleton<IQueueService<ItemEvent>, AzureItemEventQueueService>(
                (_) => new AzureItemEventQueueService(Configuration["ConnectionStrings:storage"])
            );

            services.AddSingleton<IQueueService<ItemTemplateEvent>, AzureItemTemplateEventQueueService>(
                (_) => new AzureItemTemplateEventQueueService(Configuration["ConnectionStrings:storage"])
            );

            services.AddSingleton<ICommandGenerator, CommandGenerator>();
            services.AddSingleton<IEventProcessor, EventProcessor>();

            services.AddScoped<DbContext>( (serviceProvider) => new DbContext() {
                Factory = System.Data.SqlClient.SqlClientFactory.Instance,
                ConnectionString = Configuration["ConnectionStrings:dftb"]
            });

            services.AddScoped<IItemRepository, DbItemRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "dftbsvc v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
