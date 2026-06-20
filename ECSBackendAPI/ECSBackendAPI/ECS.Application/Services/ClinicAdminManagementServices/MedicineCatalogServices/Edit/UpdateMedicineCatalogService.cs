using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ECS.Application.Services.ClinicAdminManagementServices.MedicineCatalogServices.Edit
{
    /// <summary>
    /// Handles medicine catalog editing logic by tracking user context and checking name duplications.
    /// </summary>
    public class UpdateMedicineCatalogService : IUpdateMedicineCatalogService
    {
        private readonly IRepositoryBaseAsync<MedicineCatalog, Guid, AppDbContext> _medicineRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of <see cref="UpdateMedicineCatalogService"/> with dependencies.
        /// </summary>
        /// <param name="medicineRepository">Repository for executing persistence command operations on records.</param>
        /// <param name="staffClinicRepository">Repository boundary instance managing staff to clinic links lookup maps.</param>
        /// <param name="httpContextAccessor">Accessor to safely retrieve credentials token values out of active contexts.</param>
        public UpdateMedicineCatalogService(
            IRepositoryBaseAsync<MedicineCatalog, Guid, AppDbContext> medicineRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _medicineRepository = medicineRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes medicine details alterations without using internal branching statements in the main flow block.
        /// </summary>
        /// <param name="request">The data block payload tracking properties modifications context.</param>
        /// <returns>An encapsulated standard response schema representation packaging tracking flags states.</returns>
        public async Task<ApiResponse<UpdateMedicineCatalogResponse>> Process(UpdateMedicineCatalogRequest request)
        {
            // Step 1: Initialize sequential control tracking validation state flags properties
            bool isUserValid = true;

            // Step 2: Extract identity parameters coordinates from active security token metadata pipeline via non-async helper
            var userId = RetrieveUserId(ref isUserValid);

            // Step 3: Fetch linked Clinic mapping signature matching current account identity records
            var clinicResult = await RetrieveClinicId(userId, isUserValid);
            bool isClinicExist = clinicResult.IsClinicExist;
            Guid? clinicId = clinicResult.ClinicId;

            // Step 4: Search domain databases to locate targeted medicine catalog entity row metrics
            var medicineResult = await RetrieveMedicineItem(request.Id, clinicId, isClinicExist);
            bool isMedicineExist = medicineResult.IsMedicineExist;
            MedicineCatalog? medicineItem = medicineResult.Item;

            // Step 5: Check duplication parameters to shield system lists from identical name constraints violations
            bool isNameUnique = await ValidateNameUniqueness(request.MedicineName, request.Id, clinicId, isMedicineExist);

            // Step 6: Commit state properties data changes onto persistence layers tracking verification benchmarks
            var updatedItem = await SaveMedicineState(medicineItem, request, isNameUnique);

            // Step 7: Map persistence properties models into serialized response schemas data formats
            var result = MapToResponse(updatedItem);

            // Step 8: Build standardized application payload packaging status conditions tracking layouts
            return CreateResponse(result, isUserValid, isClinicExist, isMedicineExist, isNameUnique);
        }

        /// <summary>
        /// Decodes primary token context to parse out logged user Guid identifiers markers parameters.
        /// This is a synchronous method, so using ref parameters is fully valid.
        /// </summary>
        private Guid RetrieveUserId(ref bool isUserValid)
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                isUserValid = false;
                return Guid.Empty;
            }
            return userId;
        }

        /// <summary>
        /// Fetches matching clinic context records linked directly onto the current operator configuration structures.
        /// Returns a state tuple to bypass the async ref restriction.
        /// </summary>
        private async Task<(Guid? ClinicId, bool IsClinicExist)> RetrieveClinicId(Guid userId, bool isUserValid)
        {
            if (!isUserValid)
            {
                return (null, false);
            }

            var staffClinics = await _staffClinicRepository
                .FindByCondition(x => x.UserId == userId && x.IsActive)
                .ToListAsync();

            var staffClinic = staffClinics.FirstOrDefault();

            if (staffClinic == null)
            {
                return (null, false);
            }

            return (staffClinic.ClinicId, true);
        }

        /// <summary>
        /// Queries persistence layers to lookup specified medicine catalog entity criteria lines.
        /// Returns a state tuple to bypass the async ref restriction and resolves compiler ambiguity using explicit typing.
        /// </summary>
        private async Task<(MedicineCatalog? Item, bool IsMedicineExist)> RetrieveMedicineItem(Guid id, Guid? clinicId, bool isClinicExist)
        {
            if (!isClinicExist || !clinicId.HasValue)
            {
                return (null, false);
            }

            var items = await _medicineRepository
                .FindByCondition(x => x.Id == id && x.ClinicId == clinicId.Value)
                .ToListAsync();

            var item = items.FirstOrDefault();

            if (item == null)
            {
                return (null, false);
            }

            return (item, true);
        }

        /// <summary>
        /// Evaluates name collision constraints inside a specific environment area boundary matrix scope.
        /// Explicitly specifies type parameters to avoid compiler inference ambiguity.
        /// </summary>
        private async Task<bool> ValidateNameUniqueness(string name, Guid currentId, Guid? clinicId, bool isMedicineExist)
        {
            if (!isMedicineExist || !clinicId.HasValue)
            {
                return true;
            }

            var normalizedName = name.Trim().ToLower();
            var isDuplicate = await _medicineRepository
                .FindByCondition(x => x.ClinicId == clinicId.Value && x.Id != currentId && x.MedicineName.ToLower() == normalizedName)
                .AnyAsync<MedicineCatalog>();

            return !isDuplicate;
        }

        /// <summary>
        /// Modifies physical properties tracking details and updates backing operational repositories contexts.
        /// </summary>
        private async Task<MedicineCatalog?> SaveMedicineState(MedicineCatalog? item, UpdateMedicineCatalogRequest request, bool isNameUnique)
        {
            if (item == null || !isNameUnique)
            {
                return null;
            }

            item.MedicineName = request.MedicineName;
            item.GenericName = request.GenericName;
            item.Unit = request.Unit;
            item.DosageForm = request.DosageForm;
            item.Concentration = request.Concentration;
            item.Manufacturer = request.Manufacturer;
            item.Notes = request.Notes;
            item.IsActive = request.IsActive;
            item.UpdatedAt = DateTime.UtcNow;

            await _medicineRepository.UpdateAsync(item);
            await _medicineRepository.SaveChangesAsync();

            return item;
        }

        /// <summary>
        /// Projects persistent context properties values into serialization data schemas targets.
        /// </summary>
        private UpdateMedicineCatalogResponse? MapToResponse(MedicineCatalog? entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new UpdateMedicineCatalogResponse
            {
                // Mapping physical key configuration signatures tracking system rows indices
                Id = entity.Id,
                // Passing core title description string markers
                MedicineName = entity.MedicineName,
                // Assigning status flags switches tracking operational properties
                IsActive = entity.IsActive,
                // Transferring audit timeline stamp updates benchmarks metadata
                UpdatedAt = entity.UpdatedAt
            };
        }

        /// <summary>
        /// Generates an encapsulation framework structure response payload containing execution outcome details.
        /// </summary>
        private ApiResponse<UpdateMedicineCatalogResponse> CreateResponse(
            UpdateMedicineCatalogResponse? result,
            bool isUserValid,
            bool isClinicExist,
            bool isMedicineExist,
            bool isNameUnique)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isClinicExist, isMedicineExist, isNameUnique);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<UpdateMedicineCatalogResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result!);
        }

        /// <summary>
        /// Screen error verification conditions to route structured framework systemic operational errors codes.
        /// </summary>
        private ApiResponse<UpdateMedicineCatalogResponse>? CreateErrorResponse(
            bool isUserValid,
            bool isClinicExist,
            bool isMedicineExist,
            bool isNameUnique)
        {
            if (!isUserValid)
            {
                return ApiResponse<UpdateMedicineCatalogResponse>.Fail(GeneralCode.APP_MESSAGE_4001.ToString());
            }
            if (!isClinicExist)
            {
                return ApiResponse<UpdateMedicineCatalogResponse>.Fail(GeneralCode.APP_MESSAGE_4014.ToString());
            }
            if (!isMedicineExist)
            {
                // Specified item not found in the clinic system catalog list context
                return ApiResponse<UpdateMedicineCatalogResponse>.Fail(GeneralCode.APP_MESSAGE_4020.ToString());
            }
            if (!isNameUnique)
            {
                // Input name value triggers active unique layout collisions validation issues
                return ApiResponse<UpdateMedicineCatalogResponse>.Fail(GeneralCode.APP_MESSAGE_4019.ToString());
            }

            return null;
        }
    }
}