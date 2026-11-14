using AspNetCore.WebSocket.RESTfulAPI.Configurations;
using AspNetCore.WebSocket.RESTfulAPI.JWT.Middlewares;
using AspNetCore.WebSocket.RESTfulAPI.JWT.TestServer.Configurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;

namespace AspNetCore.WebSocket.RESTfulAPI.JWT.TestServer
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
            services.AddWebSocketServices();

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
                    Description = "WebSocket request error codes: " +
                    "101) Request body contains invalid JSON; " +
                    "102) Required request property 'id' or 'method' is missing or empty; " +
                    "103) Method name has an invalid format; expected 'Controller.Method'; " +
                    "104) Controller discovery failed during server endpoint initialization; " +
                    "105) WebSocket connection is not registered or is no longer authenticated; " +
                    "106) One or more method parameters are missing or invalid; " +
                    "107) Requested WebSocket method was not found; " +
                    "108) Controller activation failed; " +
                    "109) WebSocket endpoint configuration is invalid; " +
                    "110) Method execution failed; the client receives a safe, non-sensitive server-error message. " +
                    "Controller discovery and activation errors may instead fail server startup or be reported as a safe 110 response.",
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
            app.WebSocketRESTfulApiJwt("/WSMessenger", receiveBufferSize: 5, keepAliveInterval: 30);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
