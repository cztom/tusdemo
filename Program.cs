using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Threading;
using tusdotnet;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using tusdotnet.Models.Expiration;
using tusdotnet.Stores;
using static System.Formats.Asn1.AsnWriter;

namespace TusDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.ConfigureKestrel((context, options) =>
            {
                options.Limits.MaxRequestBodySize = long.MaxValue;
            });

            // ��ӿ�������ͼ
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // ���ùܵ�
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
             
                app.UseHsts();
            }
            // ����tus ����
            app.UseTus(context=> CreateTusConfiguration(builder));

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}"
                );

            app.Run();
        }

        private static DefaultTusConfiguration CreateTusConfiguration(WebApplicationBuilder builder)
        {
            var env = builder.Environment.WebRootPath;

            //�ļ��ϴ�·��
            var tusFiles = Path.Combine(env, "tusfiles");

            if (!Directory.Exists(tusFiles))
            {
                Directory.CreateDirectory(tusFiles);
            }

            return new DefaultTusConfiguration() {
                UrlPath= "/uploadfile",
                //�ļ��洢·��
                Store = new TusDiskStore(tusFiles),
                //Ԫ�����Ƿ������ֵ
                MetadataParsingStrategy = MetadataParsingStrategy.AllowEmptyValues,
                //�ļ����ں��ٸ���
                Expiration = new AbsoluteExpiration(TimeSpan.FromMinutes(5)),
                //�¼����������¼������������裩
                Events = new Events
                {
                    //�ϴ�����¼��ص�
                    OnFileCompleteAsync = async ctx =>
                    {
                        //��ȡ�ϴ��ļ�
                        var file = await ctx.GetFileAsync();

                        //��ȡ�ϴ��ļ�Ԫ����
                        var metadatas = await file.GetMetadataAsync(ctx.CancellationToken);

                        //��ȡ�����ļ�Ԫ�����е�Ŀ���ļ�����
                        var fileNameMetadata = metadatas["name"];

                        //Ŀ���ļ�����base64���룬����������Ҫ����
                        var fileName = fileNameMetadata.GetString(Encoding.UTF8);

                        var extensionName = Path.GetExtension(fileName);

                        //���ϴ��ļ�ת��Ϊʵ��Ŀ���ļ�
                        File.Move(Path.Combine(tusFiles, ctx.FileId), Path.Combine(tusFiles, $"{ctx.FileId}{extensionName}"));

                        var terminationStore = ctx.Store as ITusTerminationStore;
                        await terminationStore!.DeleteFileAsync(file.Id, ctx.CancellationToken);

                    }
                }
            };
        }






 

    }
}