using Business.Core;
using Business.Core.ICore;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class CarimboCoreTests
    {
        [Theory]
        [MemberData(nameof(PdfsSemCarimbo))]
        public async Task o_pdf_nao_deve_conter_carimbo(string filePath)
        {
            // Arrange
            string carimbo = @"20[0-9]{2}-[0-9B-DF-HJ-NP-TV-Z]{6} - E-DOCS .* [0-9]{2}/[0-9]{2}/20[0-9]{2} [0-9]{2}:[0-9]{2} .* PÁGINA";
            InputFile inputFile = await CreateInputFile(filePath);
            Mock<IJsonData> mock = new Mock<IJsonData>();
            CarimboCore carimboCore = new CarimboCore(mock.Object);

            // Act
            string carimboAntes = carimboCore.BuscarExpressoesRegulares(inputFile.FileBytes, new List<string>() { carimbo }, new List<int>());

            // Assert
            carimboAntes.Should().BeNullOrWhiteSpace();
        }

        [Theory]
        [MemberData(nameof(PdfsComCarimbo))]
        public async Task contar_2_ocorrencias_para_cada_documento(string filePath)
        {
            // Arrange
            InputFile inputFile = await CreateInputFile(filePath);
            string expressaoRegularCarimbo = @"20[0-9]{2}-[0-9B-DF-HJ-NP-TV-Z]{6} - E-DOCS .* [0-9]{2}/[0-9]{2}/20[0-9]{2} [0-9]{2}:[0-9]{2} .* PÁGINA";
            Mock<IJsonData> mock = new Mock<IJsonData>();
            CarimboCore carimboCore = new CarimboCore(mock.Object);

            // Act
            IEnumerable<KeyValuePair<string, int>> carimbos = await carimboCore.RegularExpressionMatchCounter(inputFile, expressaoRegularCarimbo);

            // Assert
            carimbos.Should().NotBeNullOrEmpty();
            foreach (KeyValuePair<string, int> carimbo in carimbos)
            {
                carimbo.Value.Should().Be(2);
            }
        }

        [Theory]
        [MemberData(nameof(PdfsComCarimbo))]
        public async Task Remover_Carimbo_Lateral_Direita(string filePath)
        {
            // Arrange
            string carimbo = @"20[0-9]{2}-[0-9B-DF-HJ-NP-TV-Z]{6} - E-DOCS .* [0-9]{2}/[0-9]{2}/20[0-9]{2} [0-9]{2}:[0-9]{2} .* PÁGINA";
            InputFile inputFile = await CreateInputFile(filePath);
            Mock<IJsonData> mock = new Mock<IJsonData>();
            CarimboCore carimboCore = new CarimboCore(mock.Object);
            string carimboAntes = carimboCore.BuscarExpressoesRegulares(inputFile.FileBytes, new List<string>() { carimbo }, new List<int>());
            carimboAntes.Should().NotBeNullOrEmpty();

            // Act
            byte[] result = await carimboCore.RemoverCarimboLateral(inputFile, 0.025f, 20f);

            // Assert
            string carimboEncontrado = carimboCore.BuscarExpressoesRegulares(result, new List<string>() { carimbo }, new List<int>());
            carimboEncontrado.Should().BeNull();
        }

        private async Task<InputFile> CreateInputFile(string filePath)
        {
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            InputFile inputFile = new InputFile() { FileBytes = fileBytes };
            return inputFile;
        }

        public static List<object[]> PdfsComCarimbo()
        {
            return new List<object[]>
            {
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Capturados em DEV\A0.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Capturados em DEV\A1.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Capturados em DEV\A2.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Capturados em DEV\A3.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Capturados em DEV\A4.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Capturados em DEV\A5.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Capturados em DEV\A6.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Capturados em DEV\A7.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Capturados em DEV\A8.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Capturados em DEV\A9.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Capturados em DEV\A10.pdf" }
            };
        }

        public static List<object[]> PdfsSemCarimbo()
        {
            return new List<object[]>
            {
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Original\A0.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Original\A1.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Original\A2.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Original\A3.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Original\A4.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Original\A5.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Original\A6.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Original\A7.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Original\A8.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Original\A9.pdf" },
                new object[] { @"C:\Users\Marcelo\Downloads\Laranja\Original\A10.pdf" }
            };
        }
    }
}