using ArenaApplication.Dtos.ProfileDtos;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.IServices
{
    public interface IProfileService
    {
        Task<Result<GetProfileDto>> GetProfileAsync(Guid userId);
        Task<Result> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
    }
}
