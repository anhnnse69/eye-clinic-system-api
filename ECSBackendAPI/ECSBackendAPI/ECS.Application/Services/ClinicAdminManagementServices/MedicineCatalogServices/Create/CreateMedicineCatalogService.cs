using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECS.Application.Services.ClinicAdminManagementServices.MedicineCatalogServices.Create
{
    /// <summary>
    /// Handles the persistent persistence creation operations by parsing security tokens, verifying clinic boundaries, and enforcing name uniqueness constraints.
    /// </summary>
    public class CreateMedicineCatalogService : ICreateMedicineCatalogService
    {
        private readonly IRepositoryBaseAsync<MedicineCatalog, Guid, AppDbContext> _medicineRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of <see cref="CreateMedicineCatalogService"/> with required infrastructure boundaries.
        /// </summary>
        /// <param name="medicineRepository">Repository for persisting tracking data objects.</param>
        /// <param name="staffClinicRepository">Repository managing relationship association details.</param>
        /// <param name="httpContextAccessor">Accessor layer querying incoming identity context parameters.</param>
        public CreateMedicineCatalogService(
            IRepositoryBaseAsync<MedicineCatalog, Guid, AppDbContext> medicineRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _medicineRepository = medicineRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes internal request sequences sequentially avoiding inline conditional state breaks.
        /// </summary>
        /// <param name="request">The data model parameters configuration fields.</param>
        /// <returns>A structured envelope payload capsule.</returns>
        public async Task<ApiResponse<CreateMedicineCatalogResponse>> Process(CreateMedicineCatalogRequest request)
        {
            // Step 1: Initialize operational control verification markers
            bool isUserValid = true;

            // Step 2: Extract principal signature identities tracking current request pipelines
            var userId = RetrieveUserId(out isUserValid);

            // Step 3: Parse related infrastructure bounds to resolve matching environment identifiers
            var clinicId = await RetrieveClinicId(userId, isUserValid);

            // Step 4: Verify that the medicine name does not already exist within the active clinic boundary
            bool isMedicineUnique = await IsMedicineNameUnique(request.MedicineName, clinicId, isUserValid);

            // Step 5: Map incoming model attributes directly onto entity domain context layout representations
            var targetEntity = MapToEntity(request, clinicId ?? Guid.Empty);

            // Step 6: Save compiled structural database configurations down into physical layers
            var createdId = await PersistMedicineEntity(targetEntity, clinicId.HasValue && isMedicineUnique);

            // Step 7: Package tracking records onto isolated output data transportation models
            var result = MapToResponseDto(createdId);

            // Step 8: Formulate execution outcome response objects wrapping standard meta flags
            return CreateResponse(result, isUserValid, clinicId.HasValue, isMedicineUnique);
        }

        /// <summary>
        /// Extracts tracking keys off core environment principal validation context streams.
        /// </summary>
        /// <param name="isUserValid">Status flag mapping parameters context outcome checks.</param>
        /// <returns>The resolved unique identifier matching user signature guidelines.</returns>
        private Guid RetrieveUserId(out bool isUserValid)
        {
            isUserValid = true;
            var userIdClaim = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                isUserValid = false;
                return Guid.Empty;
            }
            return userId;
        }

        /// <summary>
        /// Probes system relation maps to isolate the clinic linked directly onto the active administrator account.
        /// </summary>
        /// <param name="userId">The system security authentication key.</param>
        /// <param name="isUserValid">Guard condition state controlling execution skip tracks.</param>
        /// <returns>The environment indicator GUID parameters block matching internal allocations.</returns>
        private async Task<Guid?> RetrieveClinicId(Guid userId, bool isUserValid)
        {
            if (!isUserValid)
            {
                return null;
            }

            // Explicitly defining the generic parameter type to resolve inference conflicts
            var staffClinic = await _staffClinicRepository
                .FindByCondition(x => x.UserId == userId && x.IsActive)
                .FirstOrDefaultAsync<StaffClinic>();

            return staffClinic?.ClinicId;
        }

        /// <summary>
        /// Validates that the requested medicine name does not duplicate an active catalog record inside the clinic.
        /// </summary>
        /// <param name="medicineName">The text string representing the main medicine title.</param>
        /// <param name="clinicId">The clinic unique identification token identifier context.</param>
        /// <param name="isUserValid">Pre-condition checker state to bypass querying upon invalid accounts.</param>
        /// <returns>True if the medicine name is unique or evaluation is skipped; otherwise, false.</returns>
        private async Task<bool> IsMedicineNameUnique(string medicineName, Guid? clinicId, bool isUserValid)
        {
            if (!isUserValid || !clinicId.HasValue || string.IsNullOrWhiteSpace(medicineName))
            {
                return true;
            }

            // Explicit generic type argument declaration to avoid compilation ambiguity
            var existingMedicine = await _medicineRepository
                .FindByCondition(x => x.ClinicId == clinicId.Value && x.MedicineName.ToLower() == medicineName.Trim().ToLower() && x.IsActive)
                .FirstOrDefaultAsync<MedicineCatalog>();

            return existingMedicine == null;
        }

        /// <summary>
        /// Persists complete domain target models down to data layers securely under guard rules.
        /// </summary>
        /// <param name="entity">The fully assembled data configuration model.</param>
        /// <param name="isValidContext">Indicates if relational validation preconditions matched constraints.</param>
        /// <returns>The tracking identifier generated out of physical insertion lines.</returns>
        private async Task<Guid> PersistMedicineEntity(MedicineCatalog entity, bool isValidContext)
        {
            if (!isValidContext)
            {
                return Guid.Empty;
            }

            var id = await _medicineRepository.CreateAsync(entity);
            await _medicineRepository.SaveChangesAsync();
            return id;
        }

        /// <summary>
        /// Transforms incoming serialization structures into persistent domain core representations.
        /// </summary>
        /// <param name="request">The transportation parameters model wrapper.</param>
        /// <param name="clinicId">The contextual domain owner tracking token identifier.</param>
        /// <returns>An un-tracked fully populated physical database object context instance.</returns>
        private MedicineCatalog MapToEntity(CreateMedicineCatalogRequest request, Guid clinicId)
        {
            return new MedicineCatalog
            {
                // Assigning a brand new distinct tracking key
                Id = Guid.NewGuid(),
                // Binding the resolved target environment operational boundary
                ClinicId = clinicId,
                // Setting up the exact primary medicine text signature
                MedicineName = request.MedicineName,
                // Mapping secondary scientific chemical reference fields
                GenericName = request.GenericName,
                // Passing down quantitative standard structural units
                Unit = request.Unit,
                // Specifying physical administration delivery details
                DosageForm = request.DosageForm,
                // Aligning technical active ingredient chemical measurements
                Concentration = request.Concentration,
                // Identifying industrial origin corporate creators
                Manufacturer = request.Manufacturer,
                // Appending dynamic supplementary tracking text blocks
                Notes = request.Notes,
                // Enabling visibility within active working domain states
                IsActive = true,
                // Recording chronological baseline system initiation times
                CreatedAt = DateTime.UtcNow,
                // Initializing tracking change modifications timestamps
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Projects resulting execution parameters back onto serialization schema models.
        /// </summary>
        /// <param name="id">The generated unique record index tracker key.</param>
        /// <returns>The structured payload object details instance block.</returns>
        private CreateMedicineCatalogResponse MapToResponseDto(Guid id)
        {
            return new CreateMedicineCatalogResponse
            {
                // Mapping system primary key identity data onto string object payload field formats
                Id = id.ToString()
            };
        }

        /// <summary>
        /// Formulates system api response envelopes according to structural execution state flags.
        /// </summary>
        /// <param name="result">The payload data containing primary details.</param>
        /// <param name="isUserValid">Flag mapping request user parameter confirmation matches.</param>
        /// <param name="isClinicExist">Flag tracking clinic allocation data visibility metrics.</param>
        /// <param name="isMedicineUnique">Flag tracking duplicate medicine validation states.</param>
        /// <returns>The completed transport envelope schema object wrapper.</returns>
        private ApiResponse<CreateMedicineCatalogResponse> CreateResponse(
            CreateMedicineCatalogResponse result,
            bool isUserValid,
            bool isClinicExist,
            bool isMedicineUnique)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isClinicExist, isMedicineUnique);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<CreateMedicineCatalogResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result);
        }

        /// <summary>
        /// Evaluates runtime processing flags to return standardized system code payloads.
        /// </summary>
        /// <param name="isUserValid">Validates identity parameter parsed metrics checks.</param>
        /// <param name="isClinicExist">Validates relational environment boundary mappings context indicators.</param>
        /// <param name="isMedicineUnique">Validates duplicate medicine naming metrics.</param>
        /// <returns>A failed API standard package if rule violations trip; otherwise null.</returns>
        private ApiResponse<CreateMedicineCatalogResponse>? CreateErrorResponse(
            bool isUserValid,
            bool isClinicExist,
            bool isMedicineUnique)
        {
            if (!isUserValid)
            {
                return ApiResponse<CreateMedicineCatalogResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4001.ToString());
            }

            if (!isClinicExist)
            {
                return ApiResponse<CreateMedicineCatalogResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4020.ToString());
            }

            if (!isMedicineUnique)
            {
                return ApiResponse<CreateMedicineCatalogResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4015.ToString());
            }

            return null;
        }
    }
}