using AspNetCore.WebSocket.RESTfullAPI.JWT.TestServer.Configurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace AspNetCore.WebSocket.RESTfullAPI.JWT.TestServer
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
            services.AddAuthentications();
            services.AddWebSocketManager();

            #region API Documents

            services.AddSwaggerGen(c =>
            {
                c.EnableAnnotations();
                c.SwaggerDoc("APIs", new OpenApiInfo
                {
                    Title = "RESTful APIs documentation",
                    Version = "v1"
                });
                c.SwaggerDoc("WS", new OpenApiInfo
                {
                    Title = "WebSocket APIs documentation",
                    Description = "The server will return some error codes before starting the WebSocket APIs: " +
                    "101) The problem on excuting Websocket's request; " +
                    "102) Request's Id or method is empty; " +
                    "103) Websocket request will support only two levels methods; " +
                    "104) Sended method's class is invalid; " +
                    "105) The user doesn't exist with sended WS data; " +
                    "106) Sended method's parameters is invalid; ",
                    Version = "v1"
                });
            });

            #endregion

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                app.UseSwagger(o =>
                {
                    o.RouteTemplate = "docs/{documentName}/docs.json";
                    o.RouteTemplate = "docs/{documentName}/docs.json";
                });

                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/docs/WS/docs.json", "WebSocket APIs");
                    c.SwaggerEndpoint("/docs/APIs/docs.json", "RESTful APIs");
                    c.RoutePrefix = "docs";
                });
            }

            app.UseRouting();
            app.UseAuthorization();
            app.WebSocketRESTfullJWT("/WSMessenger", receiveBufferSize: 5, keepAliveInterval: 30);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
