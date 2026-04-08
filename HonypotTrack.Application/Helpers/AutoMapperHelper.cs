using AutoMapper;

namespace HonypotTrack.Application.Helpers;

public static class AutoMapperHelper
{
    private static readonly Lazy<IMapper> _mapper = new(CreateMapper);

    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MapperProfiles.UsuarioProfile>();
            cfg.AddProfile<MapperProfiles.CuentaProfile>();
            cfg.AddProfile<MapperProfiles.CategoriaProfile>();
            cfg.AddProfile<MapperProfiles.ContactProfile>();
            cfg.AddProfile<MapperProfiles.TransaccionProfile>();
        });

        config.AssertConfigurationIsValid();
        return config.CreateMapper();
    }

    public static IMapper Instance => _mapper.Value;

    public static TDestination Map<TDestination>(object source) => Instance.Map<TDestination>(source);

    public static TDestination Map<TSource, TDestination>(TSource source) => Instance.Map<TSource, TDestination>(source);

    public static IEnumerable<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source) =>
        Instance.Map<IEnumerable<TDestination>>(source);
}
