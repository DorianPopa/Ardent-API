using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ardent_API.Security;
using Ardent_API.Services;
using Ardent_API.Repositories;
using Microsoft.AspNetCore.Http;

namespace Ardent_API
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
            services.AddCors(options => options.AddPolicy("Cors", builder =>
            {
                builder.
                AllowAnyOrigin().
                AllowAnyMethod().
                AllowAnyHeader();
            }));

            services.AddDbContext<DatabaseContext>(options =>
                options.UseSqlServer(@"Data Source=XPS-9560\SQLEXPRESS;Initial Catalog=Ardent;Integrated Security=True;Pooling=False")
            );

            services.AddControllers();

            /*
             * Utilitary services
             */
            
            services.AddTransient<IAuthenticationService, AuthenticationService>();
            services.AddTransient<UserService>();
            services.AddTransient<ProjectService>();
            // Endpoint user and project services

            /*
             * Repository services
             */

            services.AddTransient<UserRepository>();
            services.AddTransient<ProjectRepository>();

            /*
            services.AddHttpsRedirection(options => {
                options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
                options.HttpsPort = 5001;
            });
            */
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors("Cors");

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
