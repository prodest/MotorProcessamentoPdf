using API.Models;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace API.StartupConfigurations
{
    public static class AutomapperConfiguration
    {
        public static IServiceCollection ConfigurarAutomapper(this IServiceCollection services)
        {
            // configurar automapper
            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<InputFileDto, InputFile>().ConvertUsing<InputFileDTOToInputFile>();
                cfg.CreateMap<IFormFile, byte[]>().ConvertUsing<IFormFileToByteArray>();
            });

            IMapper mapper = config.CreateMapper();
            services.AddSingleton(mapper);

            return services;
        }

        public class InputFileDTOToInputFile : ITypeConverter<InputFileDto, InputFile>
        {
            public InputFile Convert(InputFileDto source, InputFile destination, ResolutionContext context)
            {
                InputFile inputFile = new InputFile();
                inputFile.FileUrl = source.FileUrl;
                
                // TODO(Marcelo): Pesquisar a possibilidade de usar async
                if(source.FileBytes != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        source.FileBytes.CopyTo(memoryStream);
                        inputFile.FileBytes = memoryStream.ToArray(); 
                        memoryStream.Close();
                    }
                }

                return inputFile;
            }
        }

        public class IFormFileToByteArray : ITypeConverter<IFormFile, byte[]>
        {
            public byte[] Convert(IFormFile source, byte[] destination, ResolutionContext context)
            {
                byte[] byteArray;
                using (var memoryStream = new MemoryStream())
                {
                    source.CopyTo(memoryStream);
                    byteArray = memoryStream.ToArray();
                    memoryStream.Close();
                }

                return byteArray;
            }
        }
    }
}
