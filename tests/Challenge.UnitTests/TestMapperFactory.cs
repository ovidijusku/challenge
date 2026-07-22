using AutoMapper;
using Challenge.Core.Mapping;
using Microsoft.Extensions.Logging.Abstractions;

namespace Challenge.UnitTests;

/// <summary>Builds a real IMapper from the production mapping profile for use in tests.</summary>
internal static class TestMapperFactory
{
    public static IMapper Create()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        return configuration.CreateMapper();
    }
}
