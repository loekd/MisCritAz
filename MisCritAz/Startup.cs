﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MisCritAz.Messaging;
using MisCritAz.Storage;

namespace MisCritAz
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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            //a place to store received messages
            services.AddSingleton<IMemoryCache, MemoryCache>();

            //register the message sender as singleton, to allow reuse of circuit breaker
            services.AddSingleton<IServiceBusMessageSender>(sp =>
            {
                var sender = new MultiServiceBusMessageSender(sp.GetRequiredService<IOptions<ServiceBusConnectionSettings>>());
                sender.Initialize();
                return sender;
            });

            //register the message receiver as hosted service, so it shares its lifecycle with the process
            services.AddHostedService<MultiServiceBusMessageReceiver>();
            


            //register the blob sender as singleton, to allow reuse of circuit breaker
            services.AddSingleton<IBlobWriter, MultiBlobWriter>();

            //register the blob reader as scoped, no need to keep instances alive
            services.AddScoped<IBlobReader, MultiBlobReader>();
            

            //register configuration settings
            services.Configure<ServiceBusConnectionSettings>(Configuration.GetSection("ServiceBusConnectionSettings"));
            services.Configure<BlobStorageConnectionSettings>(Configuration.GetSection("BlobStorageConnectionSettings"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
