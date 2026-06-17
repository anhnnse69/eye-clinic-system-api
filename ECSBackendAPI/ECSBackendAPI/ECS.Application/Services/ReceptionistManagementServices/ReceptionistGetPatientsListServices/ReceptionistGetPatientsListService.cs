using ECS.Application.Common.Response;
using ECS.Domain.Entities.Patient;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using System.Linq.Expressions;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetPatientsListServices
{
    /// <summary>
    /// Handles the business logic for resolving, filtering, and mapping paginated patient profiles on the system.
    /// </summary>
    public class ReceptionistGetPatientsListService : IReceptionistGetPatientsListService
    {
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientQueryRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistGetPatientsListService"/> with query repositories.
        /// </summary>
        /// <param name="patientQueryRepo">The query repository for database patient records assignment.</param>
        public ReceptionistGetPatientsListService(IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientQueryRepo)
        {
            _patientQueryRepo = patientQueryRepo;
        }

        /// <summary>
        /// Orchestrates the process of building criteria, executing queries, and extracting paginated patient data.
        /// </summary>
        /// <param name="request">The filtration and pagination arguments for the patient query.</param>
        /// <returns>A structured framework response wrapping final data arrays and pagination metadata.</returns>
        public async Task<ApiResponse<List<ReceptionistGetPatientsListResponse>>> Process(ReceptionistGetPatientsListRequest request)
        {
            // Step 1: Construct the dynamic filtration expression tree for EF Core targeting Patient entities
            var filterExpression = BuildFilterCriteria(request);
            // Step 2: Execute query to extract paginated records (sorted descendingly by latest creation date)
            var databaseRecords = ExecutePatientsQuery(filterExpression, request, out int aggregateTotal);
            // Step 3: Map entity tracking structures into flattened DTO arrays compatible with UI states
            var formattedList = MapToPresentationDto(databaseRecords);
            // Step 4: Calculate and attach pagination tracking structural metrics
            var paginationMetadata = BuildPaginationMeta(request, aggregateTotal);
            // Step 5: Wrap payload inside standard response envelope and finalize
            return CreateApiResponse(formattedList, paginationMetadata);
        }

        /// <summary>
        /// Builds dynamic expression trees targeting patient profile entities based on UI criteria parameters.
        /// </summary>
        /// <param name="request">The filters package from consumer query strings.</param>
        /// <returns>A reusable LINQ system predicate expression lambda.</returns>
        private Expression<Func<PatientProfile, bool>> BuildFilterCriteria(ReceptionistGetPatientsListRequest request)
        {
            // Default fallback initialized to true expression to retrieve all records if no active filter is supplied
            Expression<Func<PatientProfile, bool>> filter = p => true;
            var name = request.SearchName;
            var phone = request.SearchPhone;
            var hasName = !string.IsNullOrWhiteSpace(name);
            var hasPhone = !string.IsNullOrWhiteSpace(phone);
            if (hasName)
            {
                var search = name!.Trim().ToLower();
                filter = CombineExpressions(filter, p => p.FullName.ToLower().Contains(search));
            }
            if (hasPhone)
            {
                var search = phone!.Trim();
                filter = CombineExpressions(filter, p => p.PhoneNumber != null && p.PhoneNumber.Contains(search));
            }
            return filter;
        }

        /// <summary>
        /// Executes the optimized database lookup to extract a structured slice of paginated records.
        /// </summary>
        /// <param name="filterExpression">The compiled lambda filters expression.</param>
        /// <param name="request">The current requested page pagination size parameters context.</param>
        /// <param name="totalRecords">Output parameter tracing total matching rows before partition constraints.</param>
        /// <returns>A list of resolved <see cref="PatientProfile"/> data tracking fragments.</returns>
        private List<PatientProfile> ExecutePatientsQuery(
            Expression<Func<PatientProfile, bool>> filterExpression,
            ReceptionistGetPatientsListRequest request,
            out int totalRecords)
        {
            var baseQuery = _patientQueryRepo.FindByCondition(filterExpression, trackChanges: false);
            // Prioritize and order data sequences with the newest record items at the top
            baseQuery = baseQuery.OrderByDescending(p => p.CreatedAt);
            totalRecords = baseQuery.Count();
            return baseQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();
        }

        /// <summary>
        /// Maps tracking database records into presentation layer DTO sequences and formats dates for UI rendering.
        /// </summary>
        /// <param name="profiles">The raw source list tracking database values.</param>
        /// <returns>The collection containing formatted <see cref="ReceptionistGetPatientsListResponse"/> items.</returns>
        private List<ReceptionistGetPatientsListResponse> MapToPresentationDto(List<PatientProfile> profiles)
        {
            return profiles.Select(p => new ReceptionistGetPatientsListResponse
            {
                Id = p.Id.ToString(),
                FullName = p.FullName,
                Gender = p.Gender.ToString().ToUpper(), // Preserved attribute code formatting for UI data grid rendering
                Dob = p.Dob.ToString("yyyy-MM-dd"),
                IdentityNumber = p.IdentityNumber,
                Address = p.Address,
                PhoneNumber = p.PhoneNumber,
                BhytNumber = p.BhytNumber,
                BloodType = p.BloodType,
                CreatedAt = p.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }).ToList();
        }

        /// <summary>
        /// Computes and instantiates pagination framework structural metadata models.
        /// </summary>
        private MetaResponse BuildPaginationMeta(ReceptionistGetPatientsListRequest request, int totalRecords)
        {
            return new MetaResponse(request.PageNumber, request.PageSize, totalRecords);
        }

        /// <summary>
        /// Wraps the generated pagination payload elements inside standard response success framework wrappers.
        /// </summary>
        private ApiResponse<List<ReceptionistGetPatientsListResponse>> CreateApiResponse(List<ReceptionistGetPatientsListResponse> resultList, MetaResponse meta)
        {
            return ApiResponse<List<ReceptionistGetPatientsListResponse>>.Success("APP_MESSAGE_2000", resultList, meta);
        }

        /// <summary>
        /// Utility logical helper to append independent query predicate expressions together using the conditional AndAlso model.
        /// </summary>
        private Expression<Func<T, bool>> CombineExpressions<T>(
            Expression<Func<T, bool>> first,
            Expression<Func<T, bool>> second)
        {
            var parameter = Expression.Parameter(typeof(T));
            var leftVisitor = new ReplaceExpressionVisitor(first.Parameters[0], parameter);
            var left = leftVisitor.Visit(first.Body);
            var rightVisitor = new ReplaceExpressionVisitor(second.Parameters[0], parameter);
            var right = rightVisitor.Visit(second.Body);
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left!, right!), parameter);
        }
    }

    /// <summary>
    /// Utility internal expression visitor to map independent evaluation parameters down to a uniform shared lambda root.
    /// </summary>
    internal class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;
        public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldValue ? _newValue : base.VisitParameter(node);
        }
    }
}