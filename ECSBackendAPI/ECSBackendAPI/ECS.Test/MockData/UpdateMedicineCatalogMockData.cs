using ECS.Application.Services.ClinicAdminManagementServices.ClinicEditMedicineServices;
using ECS.Domain.Entities.Clinics;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Provides reusable mock data for UpdateMedicineCatalog unit tests.
    /// </summary>
    public static class UpdateMedicineCatalogMockData
    {
        // ── Deterministic identifiers ───────────────────────────────────────────

        public static Guid TestStaffUserId => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static Guid TestClinicId => Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static Guid TestStaffClinicId => Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static Guid TestMedicineId => Guid.Parse("55555555-5555-5555-5555-555555555555");

        // ── Aggregate helpers ──────────────────────────────────────────────────

        public static StaffClinic GetActiveStaffClinic() => new StaffClinic
        {
            Id = TestStaffClinicId,
            UserId = TestStaffUserId,
            ClinicId = TestClinicId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-1)
        };

        public static MedicineCatalog GetExistingMedicine(
            Guid? id = null,
            Guid? clinicId = null,
            string medicineName = "Paracetamol 500mg",
            bool isActive = true) => new MedicineCatalog
        {
            Id = id ?? TestMedicineId,
            ClinicId = clinicId ?? TestClinicId,
            MedicineName = medicineName,
            GenericName = "Acetaminophen",
            Unit = "Tablet",
            DosageForm = "Oral",
            Concentration = "500mg",
            Manufacturer = "Test Pharma",
            Notes = "Standard catalog entry",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow.AddYears(-1),
            UpdatedAt = DateTime.UtcNow.AddYears(-1)
        };

        // ── Request factories ──────────────────────────────────────────────────

        public static UpdateMedicineCatalogRequest GetDefaultRequest(
            Guid? id = null,
            string medicineName = "Paracetamol 500mg") =>
            new UpdateMedicineCatalogRequest
            {
                Id = id ?? TestMedicineId,
                MedicineName = medicineName,
                GenericName = "Acetaminophen",
                Unit = "Tablet",
                DosageForm = "Oral",
                Concentration = "500mg",
                Manufacturer = "Test Pharma",
                Notes = "Updated notes",
                IsActive = true
            };

        public static UpdateMedicineCatalogRequest GetMinimalRequest(
            Guid? id = null,
            string medicineName = "Ibuprofen 400mg") =>
            new UpdateMedicineCatalogRequest
            {
                Id = id ?? TestMedicineId,
                MedicineName = medicineName,
                IsActive = false
            };
    }
}
