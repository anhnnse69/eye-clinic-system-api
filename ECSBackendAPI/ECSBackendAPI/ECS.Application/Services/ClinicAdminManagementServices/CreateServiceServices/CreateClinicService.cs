using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ClinicAdminManagementServices.CreateServiceServices
{
    /// <summary>
    /// Handles the application business workflows required to append new services under a clinic administrator context.
    /// </summary>
    public class CreateService : ICreateService
    {
        private readonly IRepositoryBaseAsync<Service, Guid, AppDbContext> _serviceRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateService"/> class with infrastructure dependencies.
        /// </summary>
        public CreateService(
            IRepositoryBaseAsync<Service, Guid, AppDbContext> serviceRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _serviceRepository = serviceRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Non-conditional pipeline processing method mapping sequential operations to save data parameters safely.
        /// </summary>
        public async Task<ApiResponse<CreateServiceResponse>> Process(CreateServiceRequest request)
        {
            // Step 1: Extract identity metrics and execution tokens
            var userContext = RetrieveUserId();

            // Step 2: Search operational relational databases to identify the clinic bound tightly to the active account context
            var clinicContext = await RetrieveClinicId(userContext.UserId, userContext.IsValid);

            // Step 2.5: Đảm bảo không trùng tên dịch vụ y tế trong cùng một phòng khám
            var isDuplicate = await CheckDuplicateServiceName(request.ServiceName, clinicContext.ClinicId, clinicContext.IsValid);

            // Step 3: Map presentation layer requests directly into real physical database model structures
            // ĐÃ SỬA: Truyền đủ 2 tham số request và clinicContext.ClinicId
            var targetEntity = MapToEntity(request, clinicContext.ClinicId);

            // Step 4: Execute database mutations writing new service assets safely
            await SaveNewService(targetEntity, userContext.IsValid, clinicContext.IsValid, isDuplicate);

            // Step 5: Map database changes back onto clean presentation payload transmission contracts
            var resultPayload = MapToResponseDto(targetEntity);

            // Step 6: Analyze process parameters and compile standardized payload packages handling pipeline outcomes
            return CreateResponse(resultPayload, userContext.IsValid, clinicContext.IsValid, isDuplicate);
        }

        private (Guid UserId, bool IsValid) RetrieveUserId()
        {
            var userIdClaim = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return (Guid.Empty, false);
            }
            return (userId, true);
        }

        private async Task<(Guid ClinicId, bool IsValid)> RetrieveClinicId(Guid userId, bool isUserValid)
        {
            if (!isUserValid)
            {
                return (Guid.Empty, false);
            }

            var staffClinic = await _staffClinicRepository
                .FindByCondition(x => x.UserId == userId && x.IsActive)
                .AsQueryable()
                .FirstOrDefaultAsync<StaffClinic>();

            if (staffClinic == null)
            {
                return (Guid.Empty, false);
            }

            return (staffClinic.ClinicId, true);
        }

        /// <summary>
        /// Kiểm tra xem tên dịch vụ đã tồn tại trong phòng khám này chưa (không phân biệt chữ hoa, chữ thường và cắt khoảng trắng thừa).
        /// </summary>
        private async Task<bool> CheckDuplicateServiceName(string serviceName, Guid clinicId, bool isClinicValid)
        {
            if (!isClinicValid || string.IsNullOrWhiteSpace(serviceName))
            {
                return false;
            }

            var cleanName = serviceName.Trim().ToLower();

            return await _serviceRepository
                .FindByCondition(x => x.ClinicId == clinicId && x.ServiceName.Trim().ToLower() == cleanName && x.IsActive)
                .AsQueryable()
                .AnyAsync();
        }

        private async Task SaveNewService(Service entity, bool isUserValid, bool isClinicValid, bool isDuplicate)
        {
            if (!isUserValid || !isClinicValid || isDuplicate)
            {
                return;
            }

            await _serviceRepository.CreateAsync(entity);
            await _serviceRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Transforms serialization boundary data properties directly into physical business domains records.
        /// </summary>
        // ĐÃ SỬA: Nhận đủ 2 tham số đầu vào (CreateServiceRequest request, Guid clinicId)
        private Service MapToEntity(CreateServiceRequest request, Guid clinicId)
        {
            return new Service
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId, // ĐÃ SỬA: Khớp chính xác với tên tham số Guid clinicId bên trên
                ServiceName = request.ServiceName.Trim(),
                Price = request.Price,
                DurationMinutes = request.DurationMinutes,
                IsActive = true
            };
        }

        private CreateServiceResponse MapToResponseDto(Service entity)
        {
            return new CreateServiceResponse
            {
                Id = entity.Id,
                ServiceName = entity.ServiceName,
                ClinicId = entity.ClinicId,
                Price = entity.Price,
                DurationMinutes = entity.DurationMinutes,
                IsActive = entity.IsActive
            };
        }

        private ApiResponse<CreateServiceResponse> CreateResponse(
            CreateServiceResponse result,
            bool isUserValid,
            bool isClinicValid,
            bool isDuplicate)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isClinicValid, isDuplicate);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<CreateServiceResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result);
        }

        private ApiResponse<CreateServiceResponse>? CreateErrorResponse(bool isUserValid, bool isClinicValid, bool isDuplicate)
        {
            if (!isUserValid)
            {
                return ApiResponse<CreateServiceResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4001.ToString());
            }

            if (!isClinicValid)
            {
                return ApiResponse<CreateServiceResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4020.ToString());
            }

            if (isDuplicate)
            {
                return ApiResponse<CreateServiceResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4041.ToString()); 
            }

            return null;
        }
    }
}