using ECS.Application.Services.ClinicAdminManagementServices.ClinicCreateMedicineServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Provides reusable mock data for CreateMedicineCatalog unit tests.
    /// </summary>
    public static class CreateMedicineCatalogMockData
    {
        // ── Deterministic identifiers ───────────────────────────────────────────

        public static Guid TestStaffUserId => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static Guid TestClinicId => Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static Guid TestStaffClinicId => Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static Guid TestMedicineId => Guid.Parse("55555555-5555-5555-5555-555555555555");

        // ── Aggregate helpers ──────────────────────────────────────────────────

        public static User GetTestClinicAdminUser() => new User
        {
            Id = TestStaffUserId,
            FullName = "Nguyen Van ClinicAdmin",
            Phone = "0900000002",
            Email = "clinicadmin@ECS.vn",
            PasswordHash = "x",
            Role = UserRole.CLINIC_ADMIN,
            IsActive = true
        };

        public static Clinic GetTestClinic() => new Clinic
        {
            Id = TestClinicId,
            Name = "ECS Test Clinic",
            Address = "1 Test Street",
            Phone = "0900000099",
            Email = "clinic@ECS.vn",
            IsActive = true,
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(17, 0)
        };

        public static StaffClinic GetActiveStaffClinic() => new StaffClinic
        {
            Id = TestStaffClinicId,
            UserId = TestStaffUserId,
            ClinicId = TestClinicId,
            Role = StaffRole.CLINIC_ADMIN,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-1)
        };

        public static MedicineCatalog GetExistingMedicine(string medicineName = "Paracetamol 500mg") => new MedicineCatalog
        {
            Id = Guid.NewGuid(),
            ClinicId = TestClinicId,
            MedicineName = medicineName,
            GenericName = "Acetaminophen",
            Unit = "Tablet",
            DosageForm = "Oral",
            Concentration = "500mg",
            Manufacturer = "Test Pharma",
            IsActive = true
        };

        // ── Request factories ──────────────────────────────────────────────────

        public static CreateMedicineCatalogRequest GetDefaultRequest(string medicineName = "Paracetamol 500mg") =>
            new CreateMedicineCatalogRequest
            {
                MedicineName = medicineName,
                GenericName = "Acetaminophen",
                Unit = "Tablet",
                DosageForm = "Oral",
                Concentration = "500mg",
                Manufacturer = "Test Pharma",
                Notes = "Standard catalog entry"
            };

        public static CreateMedicineCatalogRequest GetMinimalRequest(string medicineName = "Amoxicillin 250mg") =>
            new CreateMedicineCatalogRequest
            {
                MedicineName = medicineName
            };

        public static CreateMedicineCatalogRequest GetBlankMedicineNameRequest() =>
            new CreateMedicineCatalogRequest
            {
                MedicineName = "   "
            };
    }
}
