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
            // Account Mappings
            CreateMap<Repository.Entities.Account, DTOs.ResponseAccountDto>().ReverseMap();
            CreateMap<Repository.Entities.Account, DTOs.RequestAccountDto>().ReverseMap();
            //Add other mappings here as needed
        }
    }
}
