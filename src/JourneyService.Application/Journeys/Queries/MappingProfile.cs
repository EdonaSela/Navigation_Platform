using AutoMapper;

namespace JourneyService.Application.Journeys.Queries
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<JourneyService.Domain.Entities.JourneyFavorite, JourneyFavoriteDto>();

            CreateMap<JourneyService.Domain.Entities.Journey, JourneyDto>()
                .ForCtorParam("DistanceKm", opt => opt.MapFrom(src => src.Distance.Value))
                .ForCtorParam("TransportType", opt => opt.MapFrom(src => src.TransportType.ToString()))
                .ForCtorParam("PublicSharingToken", opt => opt.MapFrom(src => src.PublicSharingToken))
                .ForCtorParam("IsPublicLinkRevoked", opt => opt.MapFrom(src => src.IsPublicLinkRevoked))
                .ForCtorParam("Favorites", opt => opt.MapFrom(src => src.Favorites));
        }
    }
}