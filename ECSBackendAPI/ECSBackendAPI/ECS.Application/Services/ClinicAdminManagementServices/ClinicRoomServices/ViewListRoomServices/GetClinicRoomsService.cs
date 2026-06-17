using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.ViewListRoomServices
{
    /// <summary>
    /// Coordinates database retrieval workloads to resolve and partition facility rooms tied onto authorized admin contexts.
    /// </summary>
    public class GetClinicRoomsService : IGetClinicRoomsService
    {
        private readonly IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext> _roomRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetClinicRoomsService"/> class with required boundaries.
        /// </summary>
        /// <param name="roomRepository">Data repository instance handling entity querying pipelines for facility rooms.</param>
        /// <param name="staffClinicRepository">Data repository managing underlying staff relationship configurations.</param>
        /// <param name="httpContextAccessor">Accessor layer targeting operational security token identities.</param>
        public GetClinicRoomsService(
            IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext> roomRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _roomRepository = roomRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Dispatches data parsing sequences, evaluates access boundaries, and builds paginated matrix arrays.
        /// </summary>
        /// <param name="request">The structural criteria data wrapping filtering keywords and indexes.</param>
        /// <returns>A unified tracking capsule encompassing response arrays alongside pagination metadata blocks.</returns>
        public async Task<ApiResponse<List<GetClinicRoomResponse>>> Process(GetClinicRoomsRequest request)
        {
            // Step 1: Initialize operational control indicators tracking execution flows
            bool isUserValid = true;

            // Step 2: Extract identity attributes out of active token signature contexts
            var userId = RetrieveUserId(out isUserValid);

            // Step 3: Search persistence schemas to evaluate clinic anchors mapping to the active administrator
            var clinicId = await RetrieveClinicId(userId, isUserValid);

            // Step 4: Synthesize query restriction expression trees using incoming parameters
            var filterExpression = BuildFilterExpression(clinicId ?? Guid.Empty, request);

            // Step 5: Execute database server evaluation operations returning records matching specific index partitions
            var (rooms, totalRecords) = await ExecutePagedQuery(filterExpression, request);

            // Step 6: Transform physical persistent backend models onto specialized detached response schemas
            var result = MapToResponseDto(rooms);

            // Step 7: Formulate tracking indicators bound inside localized pagination wrappers
            var meta = BuildPaginationMeta(request, totalRecords);

            // Step 8: Build and output standardized operational response packaging layouts
            return CreateResponse(result, meta, isUserValid, clinicId.HasValue);
        }

        /// <summary>
        /// Extributes unique user keys matching data embedded within HTTP claim headers.
        /// </summary>
        /// <param name="isUserValid">Status flag altered to false if parsing constraints fail validations.</param>
        /// <returns>A parsed structural identity Guid representation.</returns>
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
        /// Retrieves mapping keys identifying the environment boundary associated with the verified operator.
        /// </summary>
        /// <param name="userId">Authorized unique systemic coordinate tracking vectors.</param>
        /// <param name="isUserValid">Validation guard blocking operational workflows if state tracking evaluates false.</param>
        /// <returns>An environment identity token key if located; otherwise null.</returns>
        private async Task<Guid?> RetrieveClinicId(Guid userId, bool isUserValid)
        {
            if (!isUserValid)
            {
                return null;
            }

            var query = _staffClinicRepository.FindByCondition(x => x.UserId == userId && x.IsActive);

            // Explicit invocation using EntityFrameworkQueryableExtensions to avoid ambiguous async generic inference errors
            var staffClinic = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(query);

            return staffClinic?.ClinicId;
        }

        /// <summary>
        /// Resolves filtering expressions using incoming keywords against target persistent entities.
        /// </summary>
        /// <param name="clinicId">Verified bounding reference anchoring search queries.</param>
        /// <param name="request">Parameters containing sorting instructions, state configurations, and filters.</param>
        /// <returns>A queryable lambda tree mapped to model evaluation pipelines.</returns>
        private Expression<Func<FacilityRoom, bool>> BuildFilterExpression(Guid clinicId, GetClinicRoomsRequest request)
        {
            var searchTerm = request.SearchTerm?.Trim().ToLower();
            var typeFilter = request.RoomType?.Trim().ToLower();

            return x =>
                x.ClinicId == clinicId
                && (string.IsNullOrEmpty(searchTerm) || x.RoomName.ToLower().Contains(searchTerm))
                && (string.IsNullOrEmpty(typeFilter) || (x.RoomType != null && x.RoomType.ToLower().Contains(typeFilter)))
                && (!request.IsActive.HasValue || x.IsActive == request.IsActive.Value);
        }

        /// <summary>
        /// Resolves paged database operations using explicit range calculations and tracking bypass flags.
        /// </summary>
        /// <param name="filterExpression">Functional conditional rules governing selection boundaries.</param>
        /// <param name="request">Data object tracking segmentation sizing limits.</param>
        /// <returns>A tuple grouping entity results alongside general item record totals.</returns>
        private async Task<(List<FacilityRoom> Rooms, int TotalRecords)> ExecutePagedQuery(
            Expression<Func<FacilityRoom, bool>> filterExpression,
            GetClinicRoomsRequest request)
        {
            var query = _roomRepository.FindByCondition(filterExpression, trackChanges: false);

            // Explicitly resolve total count via EntityFrameworkQueryableExtensions targeting IQueryable interface
            var totalRecords = await EntityFrameworkQueryableExtensions.CountAsync(query);

            var pagedQuery = query
                .OrderBy(x => x.RoomName)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);

            // Explicitly resolve resulting data set via EntityFrameworkQueryableExtensions to bypass conflicting extension paths
            var items = await EntityFrameworkQueryableExtensions.ToListAsync(pagedQuery);

            return (items, totalRecords);
        }

        /// <summary>
        /// Translates backend entity records onto decoupled application data transport entities.
        /// </summary>
        /// <param name="data">Collection list populated via database reading processes.</param>
        /// <returns>A sanitized presentation layout list built for output serialization nodes.</returns>
        private List<GetClinicRoomResponse> MapToResponseDto(List<FacilityRoom> data)
        {
            return data.Select(room => new GetClinicRoomResponse
            {
                // Formatting persistent physical model Guid onto a standard transport text layout
                Id_room = room.Id.ToString(),
                // Transporting raw room descriptive names
                RoomName = room.RoomName,
                // Assigning safety fallbacks preventing serialization discrepancies when classification types missing
                RoomType = string.IsNullOrWhiteSpace(room.RoomType) ? "General" : room.RoomType,
                // Passing operational state variables
                IsActive = room.IsActive
            }).ToList();
        }

        /// <summary>
        /// Compiles metadata layout boundaries tracking dataset segmentation scales.
        /// </summary>
        /// <param name="request">Model containing pagination metrics requested by consumers.</param>
        /// <param name="totalRecords">Overall item counts identified within database matches.</param>
        /// <returns>A populated structural calculation instance mapping tracking indices.</returns>
        private MetaResponse BuildPaginationMeta(GetClinicRoomsRequest request, int totalRecords)
        {
            return new MetaResponse(request.PageNumber, request.PageSize, totalRecords);
        }

        /// <summary>
        /// Inspects analytical metrics to structure localized transport wrappers packing output or validation updates.
        /// </summary>
        /// <param name="result">Collection payload mapping data components projected from entities.</param>
        /// <param name="meta">Calculated pagination structures defining block spans.</param>
        /// <param name="isUserValid">Status metric tracking authorization token evaluations.</param>
        /// <param name="isClinicExist">Status metric capturing workspace tracking configurations.</param>
        /// <returns>A standardized application wrapper ready for consumer transmissions.</returns>
        private ApiResponse<List<GetClinicRoomResponse>> CreateResponse(
            List<GetClinicRoomResponse> result,
            MetaResponse meta,
            bool isUserValid,
            bool isClinicExist)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isClinicExist);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<List<GetClinicRoomResponse>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result,
                meta);
        }

        /// <summary>
        /// Analyzes conditional flags to formulate targeted exception schemas if system checks fail.
        /// </summary>
        /// <param name="isUserValid">Tracks current authentication parsing execution outcomes.</param>
        /// <param name="isClinicExist">Tracks organizational link configuration mappings within lookup operations.</param>
        /// <returns>A failed API model capsule detailing identified constraints; otherwise null if clear.</returns>
        private ApiResponse<List<GetClinicRoomResponse>>? CreateErrorResponse(bool isUserValid, bool isClinicExist)
        {
            if (!isUserValid)
            {
                return ApiResponse<List<GetClinicRoomResponse>>.Fail(GeneralCode.APP_MESSAGE_4001.ToString());
            }

            if (!isClinicExist)
            {
                return ApiResponse<List<GetClinicRoomResponse>>.Fail(GeneralCode.APP_MESSAGE_4020.ToString());
            }

            return null;
        }
    }
}