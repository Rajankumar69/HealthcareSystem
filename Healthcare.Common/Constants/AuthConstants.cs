namespace Healthcare.Common.Constants
{
    public static class AuthConstants
    {
        public const string DoctorRole = "Doctor";
        public const string NurseRole = "Nurse";
        public const string PatientRole = "Patient";

        public const string HeaderUserId = "X-User-Id";
        public const string HeaderUserRole = "X-User-Role";
        public const string HeaderUserEmail = "X-User-Email";
        public const string HeaderUserName = "X-User-Name";

        public const string JwtKey = "HealthcareSuperSecretKeyForJwtAuthentication2026!Raj";
        public const string JwtIssuer = "HealthcareAuthService";
        public const string JwtAudience = "HealthcareApp";
    }
}
