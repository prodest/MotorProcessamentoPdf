using APIItextSharp.Models;
using AutoMapper;
using BusinessItextSharp.Model.CertificadoDigital;
using BusinessItextSharp.Models;
using Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;

namespace APIItextSharp.StartupConfigurations
{
    public static class AutomapperConfiguration
    {
        public static IServiceCollection ConfigurarAutomapper(this IServiceCollection services)
        {
            // configurar automapper
            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<IFormFile, Task<byte[]>>().ConvertUsing<IFormFileToByteArray>();
                cfg.CreateMap<InputFileDto, Task<InputFile>>().ConvertUsing<InputFileDTOToInputFile>();
                
                cfg.CreateMap<CertificadoDigital, CertificadoDigitalDto>();
                cfg.CreateMap<PessoaFisica, PessoaFisicaDto>();
                cfg.CreateMap<PessoaJuridica, PessoaJuridicaDto>();
            });

            IMapper mapper = config.CreateMapper();
            services.AddSingleton(mapper);

            return services;
        }

        public class InputFileDTOToInputFile : ITypeConverter<InputFileDto, Task<InputFile>>
        {
            public async Task<InputFile> Convert(InputFileDto source, Task<InputFile> destination, ResolutionContext context)
            {
                InputFile inputFile = new InputFile();

                if (source?.FileBytes != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await source.FileBytes.CopyToAsync(memoryStream);
                        inputFile.FileBytes = memoryStream.ToArray();
                        memoryStream.Close();
                    }
                }

                if (source?.FileUrl != null)
                    inputFile.FileUrl = source.FileUrl;

                return inputFile;
            }
        }

        public class IFormFileToByteArray : ITypeConverter<IFormFile, Task<byte[]>>
        {
            public async Task<byte[]> Convert(IFormFile source, Task<byte[]> destination, ResolutionContext context)
            {
                byte[] byteArray;
                using (var memoryStream = new MemoryStream())
                {
                    await source.CopyToAsync(memoryStream);
                    byteArray = memoryStream.ToArray();
                    memoryStream.Close();
                }
                return byteArray;
            }
        }
    }
}
