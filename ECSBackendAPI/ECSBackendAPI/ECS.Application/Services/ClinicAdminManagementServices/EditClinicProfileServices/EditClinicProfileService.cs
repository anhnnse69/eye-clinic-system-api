using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ClinicAdminManagementServices.EditClinicProfileServices
{
    /// <summary>
    /// Handles clinic profile update operations by verifying administrator context
    /// and persisting clinic profile modifications.
    /// </summary>
    public class EditClinicProfileService : IEditClinicProfileService
    {
        private readonly IRepositoryBaseAsync<Clinic, Guid, AppDbContext> _clinicRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of <see cref="EditClinicProfileService"/>
        /// with required dependencies.
        /// </summary>
        /// <param name="clinicRepository">
        /// Repository for persisting clinic master data updates.
        /// </param>
        /// <param name="staffClinicRepository">
        /// Repository for querying staff-to-clinic relationships.
        /// </param>
        /// <param name="httpContextAccessor">
        /// Accessor to retrieve authentication context from HTTP request.
        /// </param>
        public EditClinicProfileService(
            IRepositoryBaseAsync<Clinic, Guid, AppDbContext> clinicRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _clinicRepository = clinicRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes clinic profile update requests by validating the authenticated user,
        /// resolving the associated clinic, and applying profile modifications.
        /// </summary>
        /// <param name="request">
        /// The clinic profile update request details.
        /// </param>
        /// <returns>
        /// An <see cref="ApiResponse{Boolean}"/> indicating whether the update operation
        /// completed successfully or failed validation checks.
        /// </returns>
        public async Task<ApiResponse<bool>> Process(EditClinicProfileRequest request)
        {
            // Initialize status tracking flags
            bool isUserValid = true;
            bool isClinicExist = true;

            // Extract User ID from current token context
            var userId = RetrieveUserId(ref isUserValid);

            // Fetch linked Clinic ID based on user relationship mapping
            var clinicId = await RetrieveClinicId(userId, isUserValid);

            // Fetch clinic entity for update operation
            var retrievedClinic = await RetrieveClinicData(clinicId);

            // Validate that the targeted clinic record exists and is active
            ValidateRetrievedData(retrievedClinic, ref isClinicExist);

            // Apply incoming modifications to clinic entity
            UpdateClinic(retrievedClinic, request, isClinicExist);

            // Persist changes and generate contextual workflow response
            return await CreateResponse(retrievedClinic, isUserValid, isClinicExist);
        }

        /// <summary>
        /// Retrieves the current user's unique identifier from HTTP context JWT identity claims.
        /// </summary>
        /// <param name="isUserValid">
        /// Flag updated to <c>false</c> if the identity claim is missing or malformed.
        /// </param>
        /// <returns>
        /// The extracted <see cref="Guid"/> on success; otherwise <see cref="Guid.Empty"/>.
        /// </returns>
        private Guid RetrieveUserId(ref bool isUserValid)
        {
            var userIdClaim = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                isUserValid = false;
                return Guid.Empty;
            }

            return userId;
        }

        /// <summary>
        /// Resolves the associated Clinic ID for the specified staff/user account.
        /// </summary>
        /// <param name="userId">
        /// The verified user identifier.
        /// </param>
        /// <param name="isUserValid">
        /// Pre-condition check status indicating if user resolution is skipped.
        /// </param>
        /// <returns>
        /// The mapped <see cref="Guid"/> of the target clinic, or <c>null</c>
        /// if skipped or not found.
        /// </returns>
        private async Task<Guid?> RetrieveClinicId(
            Guid userId,
            bool isUserValid)
        {
            if (!isUserValid)
            {
                return null;
            }

            var staffClinic = await _staffClinicRepository
                .FindByCondition(
                    x => x.UserId == userId &&
                         x.IsActive)
                .FirstOrDefaultAsync();

            return staffClinic?.ClinicId;
        }

        /// <summary>
        /// Queries the data store for an active clinic record matching the specified unique identifier.
        /// </summary>
        /// <param name="clinicId">
        /// The targeted clinic identifier token.
        /// </param>
        /// <returns>
        /// The matching active <see cref="Clinic"/> entity if found;
        /// otherwise <c>null</c>.
        /// </returns>
        private async Task<Clinic?> RetrieveClinicData(Guid? clinicId)
        {
            if (!clinicId.HasValue)
            {
                return null;
            }

            return await _clinicRepository
                .FindByCondition(
                    x => x.Id == clinicId &&
                         x.IsActive,
                    true)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Evaluates the fetched clinic entity presence and flags missing data discrepancies.
        /// </summary>
        /// <param name="clinic">
        /// The resolved entity, or <c>null</c> if the look-up yielded no record.
        /// </param>
        /// <param name="isClinicExist">
        /// Flag updated to <c>false</c> if data validation checks fail.
        /// </param>
        private void ValidateRetrievedData(
            Clinic? clinic,
            ref bool isClinicExist)
        {
            if (clinic == null)
            {
                isClinicExist = false;
            }
        }

        /// <summary>
        /// Applies request values onto the resolved clinic entity.
        /// </summary>
        /// <param name="clinic">
        /// The clinic entity targeted for modification.
        /// </param>
        /// <param name="request">
        /// The incoming update request payload.
        /// </param>
        /// <param name="isClinicExist">
        /// Indicates whether clinic validation succeeded.
        /// </param>
        private static void UpdateClinic(
            Clinic? clinic,
            EditClinicProfileRequest request,
            bool isClinicExist)
        {
            if (!isClinicExist)
            {
                return;
            }

            clinic!.Name = request.Name;
            clinic.Address = request.Address;
            clinic.Phone = request.Phone;
            clinic.Email = request.Email;
            clinic.LogoUrl = request.LogoUrl;
            clinic.Description = request.Description;
            clinic.UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Generates a standardized response after processing clinic profile updates.
        /// </summary>
        /// <param name="retrievedClinic">
        /// The clinic entity being updated.
        /// </param>
        /// <param name="isUserValid">
        /// Flag indicating whether user validation succeeded.
        /// </param>
        /// <param name="isClinicExist">
        /// Flag indicating whether clinic validation succeeded.
        /// </param>
        /// <returns>
        /// A configured <see cref="ApiResponse{Boolean}"/>.
        /// </returns>
        private async Task<ApiResponse<bool>> CreateResponse(
            Clinic? retrievedClinic,
            bool isUserValid,
            bool isClinicExist)
        {
            var errorResponse = CreateErrorResponse(
                isUserValid,
                isClinicExist);

            if (errorResponse != null)
            {
                return errorResponse;
            }

            await _clinicRepository.UpdateAsync(retrievedClinic!);
            await _clinicRepository.SaveChangesAsync();

            return ApiResponse<bool>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                true);
        }

        /// <summary>
        /// Screens runtime process execution flag indicators to return standardized
        /// systemic application error schemas.
        /// </summary>
        /// <param name="isUserValid">
        /// Indicates whether user resolution parameter parsing was successful.
        /// </param>
        /// <param name="isClinicExist">
        /// Indicates whether a clinic instance entity was successfully retrieved.
        /// </param>
        /// <returns>
        /// A failed <see cref="ApiResponse{Boolean}"/> variant if errors are found;
        /// otherwise <c>null</c>.
        /// </returns>
        private ApiResponse<bool>? CreateErrorResponse(
            bool isUserValid,
            bool isClinicExist)
        {
            if (!isUserValid)
            {
                return ApiResponse<bool>.Fail(
                    GeneralCode.APP_MESSAGE_4001.ToString());
            }

            if (!isClinicExist)
            {
                return ApiResponse<bool>.Fail(
                    GeneralCode.APP_MESSAGE_4020.ToString());
            }

            return null;
        }
    }
}