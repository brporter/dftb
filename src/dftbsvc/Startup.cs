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
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

                c.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme {
                        In = ParameterLocation.Header,
                        Description = "JWT with Bearer",
                        Name = "Authorization",
                        Type = SecuritySchemeType.ApiKey
                    }
                );

                c.AddSecurityRequirement(
                    new OpenApiSecurityRequirement()
                    {
                        {
                            new OpenApiSecurityScheme() {
                                Reference = new OpenApiReference() {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            new string[] {}
                        }
                    }
                );
            });

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearerConfiguration(
                    Configuration["Jwt:Authority"],
                    Configuration["Jwt:Audience"]
                );

            services.AddSingleton<IQueueService<ItemEvent>, AzureItemEventQueueService>(
                (_) => new AzureItemEventQueueService(Configuration["ConnectionStrings:storage"])
            );

            services.AddSingleton<IQueueService<ItemTemplateEvent>, AzureItemTemplateEventQueueService>(
                (_) => new AzureItemTemplateEventQueueService(Configuration["ConnectionStrings:storage"])
            );

            services.AddSingleton<ICommandGenerator, CommandGenerator>();

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

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
