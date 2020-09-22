﻿using Business.Helpers.AssinaturaDigital;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Core.ICore
{
    public interface IAssinaturaDigitalCore
    {
        Task<IEnumerable<CertificadoDigital>> SignatureValidation(byte[] file);
        bool HasDigitalSignature(byte[] file);
    }
}
