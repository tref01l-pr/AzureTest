using Mapster;
using PdfShare.Domain.DTO.Authentication.Responses;

namespace PdfShare.Domain;

public sealed class DomainMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AuthenticationResult, AuthenticationResponse>();
    }
}
