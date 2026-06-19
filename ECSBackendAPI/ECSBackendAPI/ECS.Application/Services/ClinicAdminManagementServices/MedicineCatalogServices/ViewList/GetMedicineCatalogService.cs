using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore; // Crucial for EntityFrameworkCore async methods extensions
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ECS.Application.Services.ClinicAdminManagementServices.MedicineCatalogServices.ViewList
{
    /// <summary>
    /// Handles the operational flow for resolving, tracking, and loading clinic catalog schemas.
    /// </summary>
    public class GetMedicineCatalogService : IGetMedicineCatalogService
    {
        private readonly IRepositoryQueryBase<MedicineCatalog, Guid, AppDbContext> _medicineRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of <see cref="GetMedicineCatalogService"/> with data repository layers.
        /// </summary>
        /// <param name="medicineRepository">Repository mapping basic query executions on medicine data partitions.</param>
        /// <param name="staffClinicRepository">Repository managing contextual staff account to clinic boundaries.</param>
        /// <param name="httpContextAccessor">Accessor layer retrieving authenticated identities outside standard threads.</param>
        public GetMedicineCatalogService(
            IRepositoryQueryBase<MedicineCatalog, Guid, AppDbContext> medicineRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _medicineRepository = medicineRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the internal data pipeline workflow to parse filters, evaluate operational bounds, and return paginated data collections.
        /// </summary>
        /// <param name="request">The parameters containing data filters, keywords, and explicit pagination criteria details.</param>
        /// <returns>An API wrapper carrying metadata schemas and structural collections.</returns>
        public async Task<ApiResponse<List<GetMedicineCatalogResponse>>> Process(GetMedicineCatalogRequest request)
        {
            // Step 1: Initialize sequential control status validation variables
            bool isUserValid = true;

            // Step 2: Extract identity information parameter metrics from the active security claim session context
            var userId = RetrieveUserId(out isUserValid);

            // Step 3: Search operational relational databases to identify the clinic bound tightly to the active account
            var clinicId = await RetrieveClinicId(userId, isUserValid);

            // Step 4: Synthesize a flexible criteria lambda matrix expression using dynamic criteria logic mapping
            var filterExpression = BuildFilterExpression(clinicId ?? Guid.Empty, request);

            // Step 5: Query underlying target physical data storage blocks executing paged evaluation algorithms
            var (medicines, totalRecords) = await ExecutePagedQuery(filterExpression, request);

            // Step 6: Map internal domain state model attributes onto decoupled serialized response data schemas
            var result = MapToResponseDto(medicines);

            // Step 7: Package contextual index layout tracker parameters to formulate pagination tracking wrappers
            var meta = BuildPaginationMeta(request, totalRecords);

            // Step 8: Evaluate processing parameters and package state structures dynamically to handle execution outcomes
            return CreateResponse(result, meta, isUserValid, clinicId.HasValue);
        }

        /// <summary>
        /// Resolves the logged-in user signature coordinates using synchronous token parsing streams.
        /// </summary>
        /// <param name="isUserValid">Output evaluation status updating to false if identification variables fail parsing matches.</param>
        /// <returns>The decoded user identity descriptor parameter identifier value.</returns>
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
        /// Probes system allocation tables to isolate unique context configurations related directly onto the logged-in account.
        /// </summary>
        /// <param name="userId">The system identity identifier tracking target metrics variables.</param>
        /// <param name="isUserValid">Validation controller metric parameter guarding access execution states.</param>
        /// <returns>A tracking key token matching system database configurations if matched; otherwise null.</returns>
        private async Task<Guid?> RetrieveClinicId(Guid userId, bool isUserValid)
        {
            if (!isUserValid)
            {
                return null;
            }

            // Explicitly casting or invoking AsQueryable to safely trigger Microsoft.EntityFrameworkCore extension instead of AsyncEnumerable
            var query = _staffClinicRepository.FindByCondition(x => x.UserId == userId && x.IsActive, false).AsQueryable();
            var staffClinic = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(query);

            return staffClinic?.ClinicId;
        }

        /// <summary>
        /// Assembles dynamic lambda filter structures mapping request parameters against entity state conditions.
        /// </summary>
        /// <param name="clinicId">The contextual location domain entity reference filter identity value.</param>
        /// <param name="request">The structural entity containing filtering keys and status search parameters.</param>
        /// <returns>A composite queryable logic filter expression block.</returns>
        private Expression<Func<MedicineCatalog, bool>> BuildFilterExpression(Guid clinicId, GetMedicineCatalogRequest request)
        {
            var searchTerm = request.SearchTerm?.Trim().ToLower();

            return x => x.ClinicId == clinicId
                && (!request.IsActive.HasValue || x.IsActive == request.IsActive.Value)
                && (string.IsNullOrEmpty(searchTerm)
                    || x.MedicineName.ToLower().Contains(searchTerm)
                    || (x.GenericName != null && x.GenericName.ToLower().Contains(searchTerm))
                    || (x.Manufacturer != null && x.Manufacturer.ToLower().Contains(searchTerm)));
        }

        /// <summary>
        /// Executes database evaluations using explicit transactional tracking skips and page index limit controls.
        /// </summary>
        /// <param name="filterExpression">The structured operational logic filter condition matrix maps.</param>
        /// <param name="request">The data container tracking layout configuration bounds.</param>
        /// <returns>A tuple pairing structural result detail item list rows with overall record aggregate bounds integers.</returns>
        private async Task<(List<MedicineCatalog> Medicines, int TotalRecords)> ExecutePagedQuery(
            Expression<Func<MedicineCatalog, bool>> filterExpression,
            GetMedicineCatalogRequest request)
        {
            // Explicitly invoke AsQueryable to isolate EntityFramework Linq method routing 
            var query = _medicineRepository.FindByCondition(filterExpression, trackChanges: false).AsQueryable();

            var totalRecords = await EntityFrameworkQueryableExtensions.CountAsync(query);

            var items = await EntityFrameworkQueryableExtensions.ToListAsync(
                query.OrderBy(x => x.MedicineName)
                     .Skip((request.PageNumber - 1) * request.PageSize)
                     .Take(request.PageSize)
            );

            return (items, totalRecords);
        }

        /// <summary>
        /// Transforms internal persistent context database model list directly onto business serialization object schemas.
        /// </summary>
        /// <param name="data">The physical domain core model list array returned directly out of backend operational layers.</param>
        /// <returns>A structured target presentation data payload collection instance context.</returns>
        private List<GetMedicineCatalogResponse> MapToResponseDto(List<MedicineCatalog> data)
        {
            return data.Select(item => new GetMedicineCatalogResponse
            {
                // Mapping physical unique sequence identifier keys onto standard strings
                Id = item.Id.ToString(),
                // Transporting raw designation name schemas directly onto presentation targets
                MedicineName = item.MedicineName,
                // Assigning generic formula names
                GenericName = item.GenericName,
                // Transporting measurement metric details safely
                Unit = item.Unit,
                // Passing dosage execution layout patterns
                DosageForm = item.DosageForm,
                // Allocating density and chemical parameters directly
                Concentration = item.Concentration,
                // Attaching producing industrial plant strings
                Manufacturer = item.Manufacturer,
                // Appending specific usage advice descriptions
                Notes = item.Notes,
                // Delivering current boolean active index bounds states
                IsActive = item.IsActive,
                // Formatting timestamps onto readable presentation string paths
                CreatedAt = item.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            }).ToList();
        }

        /// <summary>
        /// Assembles pagination envelope metrics parameters using custom constructor signatures.
        /// </summary>
        /// <param name="request">The tracking item block mapping target layout page configurations.</param>
        /// <param name="totalRecords">The quantified data baseline row count metrics context.</param>
        /// <returns>A populated metadata envelope serialization module component instance.</returns>
        private MetaResponse BuildPaginationMeta(GetMedicineCatalogRequest request, int totalRecords)
        {
            return new MetaResponse(request.PageNumber, request.PageSize, totalRecords);
        }

        /// <summary>
        /// Analyzes state logic monitoring variables to determine outcome layout packaging choices.
        /// </summary>
        /// <param name="result">The structured data response payload list projected from database layers.</param>
        /// <param name="meta">The pagination metadata structure layout configuration parameters.</param>
        /// <param name="isUserValid">Indicates whether user token extraction verification matched expectations successfully.</param>
        /// <param name="isClinicExist">Indicates data layer tracking context presence attributes.</param>
        /// <returns>A standardized application payload container detailed for transport serialization layers.</returns>
        private ApiResponse<List<GetMedicineCatalogResponse>> CreateResponse(
            List<GetMedicineCatalogResponse> result,
            MetaResponse meta,
            bool isUserValid,
            bool isClinicExist)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isClinicExist);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<List<GetMedicineCatalogResponse>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result,
                meta);
        }

        /// <summary>
        /// Evaluates structural tracking conditions matrix variables to issue systemized application error entries.
        /// </summary>
        /// <param name="isUserValid">The structural state value verifying the validation of incoming authentication parameters.</param>
        /// <param name="isClinicExist">The state checking indicator capturing contextual environment existence benchmarks.</param>
        /// <returns>A failed API standard metadata package capsule if an error rule trips; otherwise null properties.</returns>
        private ApiResponse<List<GetMedicineCatalogResponse>>? CreateErrorResponse(bool isUserValid, bool isClinicExist)
        {
            if (!isUserValid)
            {
                return ApiResponse<List<GetMedicineCatalogResponse>>.Fail(
                    GeneralCode.APP_MESSAGE_4001.ToString());
            }

            if (!isClinicExist)
            {
                return ApiResponse<List<GetMedicineCatalogResponse>>.Fail(
                    GeneralCode.APP_MESSAGE_4020.ToString());
            }

            return null;
        }
    }
}