using AutoMapper;

namespace BusinessLogic.MappingProfile
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //QuizSet Mappings
            CreateMap<Repository.Entities.QuizSet, BusinessLogic.DTOs.QuizSetDtos.QuizSetRequestDto>().ReverseMap();
            CreateMap<Repository.Entities.QuizSet, BusinessLogic.DTOs.QuizSetDtos.QuizSetResponseDto>().ReverseMap();
            //Quiz Mappings
            CreateMap<Repository.Entities.Quiz, BusinessLogic.DTOs.QuizDtos.QuizRequestDto>().ReverseMap();
            CreateMap<Repository.Entities.Quiz, BusinessLogic.DTOs.QuizDtos.QuizResponseDto>().ReverseMap();
            //Role Mappings
            CreateMap<Repository.Entities.Role, BusinessLogic.DTOs.RoleDtos.ResponseRoleDto>().ReverseMap();
            CreateMap<Repository.Entities.Role, BusinessLogic.DTOs.RoleDtos.RequestRoleDto>().ReverseMap();
            // Account Mappings
            CreateMap<Repository.Entities.Account, DTOs.ResponseAccountDto>().ReverseMap();
            CreateMap<Repository.Entities.Account, DTOs.RequestAccountDto>().ReverseMap();
            
            // Quiz Attempt Mappings
            CreateMap<Repository.Entities.QuizAttempt, DTOs.ResponseQuizAttemptDto>().ReverseMap();
            CreateMap<Repository.Entities.QuizAttempt, DTOs.RequestQuizAttemptDto>().ReverseMap();
            
            // Quiz Attempt Detail Mappings
            CreateMap<Repository.Entities.QuizAttemptDetail, DTOs.ResponseQuizAttemptDetailDto>().ReverseMap();
            CreateMap<Repository.Entities.QuizAttemptDetail, DTOs.RequestQuizAttemptDetailDto>().ReverseMap();
            
            // Answer Option Mappings
            CreateMap<Repository.Entities.AnswerOption, DTOs.ResponseAnswerOptionDto>().ReverseMap();
            CreateMap<Repository.Entities.AnswerOption, DTOs.RequestAnswerOptionDto>().ReverseMap();
            
            // Pagination Mappings
            CreateMap<DTOs.PaginationRequestDto, DTOs.PaginationRequestDto>().ReverseMap();
            
            // Dashboard Mappings
            CreateMap<Repository.Entities.QuizAttempt, DTOs.DashboardDtos.QuizHistoryDto>()
                .ForMember(dest => dest.QuizName, opt => opt.MapFrom(src => src.QuizSet != null ? src.QuizSet.Title : "Unknown Quiz"))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.QuizSet != null ? src.QuizSet.Description : "General"))
                .ForMember(dest => dest.CompletedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.ScorePercentage, opt => opt.MapFrom(src => (double)src.Accuracy))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Accuracy >= 70 ? "Passed" : "Failed"))
                .ForMember(dest => dest.TimeSpent, opt => opt.MapFrom(src => src.TimeSpent ?? 0));
            
            //Add other mappings here as needed
        }
    }
}
