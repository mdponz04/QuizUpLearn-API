using AutoMapper;
using BusinessLogic.DTOs.RoleDtos;

namespace BusinessLogic.MappingProfile
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //Role Mappings
            CreateMap<Repository.Entities.Role, ResponseRoleDto>().ReverseMap();
            CreateMap<Repository.Entities.Role, RequestRoleDto>().ReverseMap();
            // Account Mappings
            CreateMap<Repository.Entities.Account, DTOs.ResponseAccountDto>().ReverseMap();
            CreateMap<Repository.Entities.Account, DTOs.RequestAccountDto>().ReverseMap();
            //Add other mappings here as needed
        }
    }
}
