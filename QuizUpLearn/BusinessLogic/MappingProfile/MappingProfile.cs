using AutoMapper;

namespace BusinessLogic.MappingProfile
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //Role Mappings
            CreateMap<Repository.Entities.Role, DTOs.ResponseRoleDto>().ReverseMap();
            CreateMap<Repository.Entities.Role, DTOs.RequestRoleDto>().ReverseMap();
            //Add other mappings here as needed
        }
    }
}
