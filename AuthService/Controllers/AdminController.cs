using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using AuthService.DTOs;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/auth/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        }

        // =========================================================================
        // 1. GET ALL USERS (Doctors, Nurses, Patients with their relations)
        // =========================================================================
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsersWithRelations()
        {
            var response = new AdminUsersResponseDto();
            var connectionString = GetConnectionString();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // A) GET DOCTORS & THEIR PATIENTS
            var doctorDict = new Dictionary<int, AdminDoctorDto>();
            var docSql = @"
                SELECT u.UserId, u.FullName, u.Email, u.PhoneNumber, u.IsActive, d.DepartmentName,
                       p.PatientID, p.FirstName + ' ' + p.LastName AS PatientName, p.Email AS PatientEmail, p.PhoneNumber AS PatientPhone,
                       a.AppointmentDate, a.AppointmentTime, a.Status, a.Reason
                FROM Users_Raj u
                LEFT JOIN Departments_Raj d ON u.DepartmentId = d.DepartmentId
                LEFT JOIN Appointments_Raj a ON u.UserId = a.DoctorId
                LEFT JOIN Patients_Raj p ON a.PatientId = p.PatientID AND p.IsDeleted = 0
                WHERE u.RoleId = 1 AND u.IsActive = 1
                ORDER BY u.UserId DESC, a.AppointmentDate DESC";

            using (var docCmd = new SqlCommand(docSql, connection))
            using (var reader = await docCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    int userId = reader.GetInt32(reader.GetOrdinal("UserId"));
                    if (!doctorDict.TryGetValue(userId, out var doc))
                    {
                        doc = new AdminDoctorDto
                        {
                            UserId = userId,
                            FullName = reader["FullName"]?.ToString() ?? string.Empty,
                            Email = reader["Email"]?.ToString() ?? string.Empty,
                            PhoneNumber = reader["PhoneNumber"] != DBNull.Value ? reader["PhoneNumber"].ToString() : null,
                            DepartmentName = reader["DepartmentName"] != DBNull.Value ? reader["DepartmentName"].ToString() : "General",
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                        };
                        doctorDict[userId] = doc;
                        response.Doctors.Add(doc);
                    }

                    if (reader["PatientID"] != DBNull.Value)
                    {
                        var apptDate = reader["AppointmentDate"] != DBNull.Value ? Convert.ToDateTime(reader["AppointmentDate"]).ToString("yyyy-MM-dd") : "";
                        doc.Patients.Add(new AdminDoctorPatientDto
                        {
                            PatientId = reader.GetInt32(reader.GetOrdinal("PatientID")),
                            PatientName = reader["PatientName"]?.ToString() ?? string.Empty,
                            Email = reader["PatientEmail"]?.ToString() ?? string.Empty,
                            PhoneNumber = reader["PatientPhone"] != DBNull.Value ? reader["PatientPhone"].ToString() : null,
                            AppointmentDate = apptDate,
                            AppointmentTime = reader["AppointmentTime"]?.ToString(),
                            Status = reader["Status"]?.ToString(),
                            Reason = reader["Reason"]?.ToString()
                        });
                    }
                }
            }

            // B) GET NURSES & THEIR PATIENTS (From recorded vitals)
            var nurseDict = new Dictionary<int, AdminNurseDto>();
            var nurseSql = @"
                SELECT u.UserId, u.FullName, u.Email, u.PhoneNumber, u.IsActive, d.DepartmentName,
                       p.PatientID, p.FirstName + ' ' + p.LastName AS PatientName, p.Email AS PatientEmail, p.PhoneNumber AS PatientPhone,
                       v.BloodPressure, v.HeartRate, v.Temperature, v.RecordedDate
                FROM Users_Raj u
                LEFT JOIN Departments_Raj d ON u.DepartmentId = d.DepartmentId
                LEFT JOIN PatientVitals_Raj v ON (v.RecordedBy LIKE '%' + SUBSTRING(u.FullName, 1, 15) + '%' OR v.RecordedBy LIKE '%' + u.Email + '%')
                LEFT JOIN Patients_Raj p ON v.PatientId = p.PatientID AND p.IsDeleted = 0
                WHERE u.RoleId = 2 AND u.IsActive = 1
                ORDER BY u.UserId DESC, v.RecordedDate DESC";

            using (var nurseCmd = new SqlCommand(nurseSql, connection))
            using (var reader = await nurseCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    int userId = reader.GetInt32(reader.GetOrdinal("UserId"));
                    if (!nurseDict.TryGetValue(userId, out var nurse))
                    {
                        nurse = new AdminNurseDto
                        {
                            UserId = userId,
                            FullName = reader["FullName"]?.ToString() ?? string.Empty,
                            Email = reader["Email"]?.ToString() ?? string.Empty,
                            PhoneNumber = reader["PhoneNumber"] != DBNull.Value ? reader["PhoneNumber"].ToString() : null,
                            DepartmentName = reader["DepartmentName"] != DBNull.Value ? reader["DepartmentName"].ToString() : "Nursing Care",
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                        };
                        nurseDict[userId] = nurse;
                        response.Nurses.Add(nurse);
                    }

                    if (reader["PatientID"] != DBNull.Value)
                    {
                        var recDate = reader["RecordedDate"] != DBNull.Value ? Convert.ToDateTime(reader["RecordedDate"]).ToString("yyyy-MM-dd HH:mm") : "";
                        nurse.Patients.Add(new AdminNursePatientDto
                        {
                            PatientId = reader.GetInt32(reader.GetOrdinal("PatientID")),
                            PatientName = reader["PatientName"]?.ToString() ?? string.Empty,
                            Email = reader["PatientEmail"]?.ToString() ?? string.Empty,
                            PhoneNumber = reader["PatientPhone"] != DBNull.Value ? reader["PatientPhone"].ToString() : null,
                            BloodPressure = reader["BloodPressure"]?.ToString(),
                            HeartRate = reader["HeartRate"] != DBNull.Value ? Convert.ToInt32(reader["HeartRate"]) : null,
                            Temperature = reader["Temperature"] != DBNull.Value ? Convert.ToDecimal(reader["Temperature"]) : null,
                            RecordedDate = recDate
                        });
                    }
                }
            }

            // C) GET ALL PATIENTS WITH THEIR ASSIGNED DOCTORS
            var patientSql = @"
                SELECT p.PatientID, p.UserId, p.Prefix, p.FirstName, p.LastName, p.Email, p.PhoneNumber,
                       p.Age, p.Gender, p.DateOfBirth, p.Problem, p.AnyMedicalHistory,
                       doc.FullName AS AssignedDoctor, a.AppointmentDate, a.Status AS AppointmentStatus
                FROM Patients_Raj p
                LEFT JOIN Users_Raj u ON p.UserId = u.UserId
                LEFT JOIN (
                    SELECT PatientId, DoctorId, AppointmentDate, Status,
                           ROW_NUMBER() OVER(PARTITION BY PatientId ORDER BY AppointmentId DESC) as rn
                    FROM Appointments_Raj
                ) a ON p.PatientID = a.PatientId AND a.rn = 1
                LEFT JOIN Users_Raj doc ON a.DoctorId = doc.UserId
                WHERE p.IsDeleted = 0
                ORDER BY p.PatientID DESC";

            using (var patCmd = new SqlCommand(patientSql, connection))
            using (var reader = await patCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var prefix = reader["Prefix"]?.ToString() ?? "";
                    var firstName = reader["FirstName"]?.ToString() ?? "";
                    var lastName = reader["LastName"]?.ToString() ?? "";
                    var fullName = $"{prefix} {firstName} {lastName}".Trim();

                    var dob = reader["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(reader["DateOfBirth"]).ToString("yyyy-MM-dd") : null;
                    var apptDate = reader["AppointmentDate"] != DBNull.Value ? Convert.ToDateTime(reader["AppointmentDate"]).ToString("yyyy-MM-dd") : null;

                    response.Patients.Add(new AdminPatientDto
                    {
                        PatientId = reader.GetInt32(reader.GetOrdinal("PatientID")),
                        UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : null,
                        FullName = fullName,
                        Email = reader["Email"]?.ToString() ?? string.Empty,
                        PhoneNumber = reader["PhoneNumber"]?.ToString() ?? string.Empty,
                        Gender = reader["Gender"] != DBNull.Value ? reader["Gender"].ToString() : "Not Specified",
                        Age = reader["Age"] != DBNull.Value ? Convert.ToInt32(reader["Age"]) : 0,
                        DateOfBirth = dob,
                        Problem = reader["Problem"] != DBNull.Value ? reader["Problem"].ToString() : null,
                        AnyMedicalHistory = reader["AnyMedicalHistory"] != DBNull.Value ? reader["AnyMedicalHistory"].ToString() : null,
                        IsActive = true,
                        AssignedDoctor = reader["AssignedDoctor"] != DBNull.Value ? reader["AssignedDoctor"].ToString() : null,
                        AppointmentDate = apptDate,
                        AppointmentStatus = reader["AppointmentStatus"] != DBNull.Value ? reader["AppointmentStatus"].ToString() : null
                    });
                }
            }

            return Ok(response);
        }

        // =========================================================================
        // 2. ADD USER (Doctor, Nurse, or Patient)
        // =========================================================================
        [HttpPost("users")]
        public async Task<IActionResult> AddUser([FromBody] AdminCreateUserDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var connectionString = GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // 1. Check if email already exists
            var checkSql = "SELECT COUNT(1) FROM Users_Raj WHERE Email = @Email";
            using (var checkCmd = new SqlCommand(checkSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@Email", dto.Email.Trim().ToLower());
                int count = (int)await checkCmd.ExecuteScalarAsync()!;
                if (count > 0)
                {
                    return BadRequest(new { message = "User with this email already exists." });
                }
            }

            // 2. Resolve RoleId
            int roleId = 1;
            if (string.Equals(dto.Role, "Nurse", StringComparison.OrdinalIgnoreCase)) roleId = 2;
            else if (string.Equals(dto.Role, "Patient", StringComparison.OrdinalIgnoreCase)) roleId = 3;

            // 3. Compute SHA256 Password Hash
            string passwordHash = ComputeSha256Hash(dto.Password);

            // 4. Validate DepartmentId to prevent FK_Users_Departments_Raj constraint violation
            object departmentIdParam = DBNull.Value;
            if (roleId != 3 && dto.DepartmentId.HasValue && dto.DepartmentId.Value > 0)
            {
                var checkDeptSql = "SELECT COUNT(1) FROM Departments_Raj WHERE DepartmentId = @DeptId";
                using var deptCmd = new SqlCommand(checkDeptSql, connection);
                deptCmd.Parameters.AddWithValue("@DeptId", dto.DepartmentId.Value);
                int deptCount = (int)(await deptCmd.ExecuteScalarAsync() ?? 0);
                if (deptCount > 0)
                {
                    departmentIdParam = dto.DepartmentId.Value;
                }
                else
                {
                    // Fallback to first available department in database
                    var fallbackDeptSql = "SELECT TOP 1 DepartmentId FROM Departments_Raj ORDER BY DepartmentId";
                    using var fallbackCmd = new SqlCommand(fallbackDeptSql, connection);
                    var fallbackId = await fallbackCmd.ExecuteScalarAsync();
                    if (fallbackId != null && fallbackId != DBNull.Value)
                    {
                        departmentIdParam = Convert.ToInt32(fallbackId);
                    }
                }
            }

            // 5. Insert into Users_Raj
            int newUserId = 0;
            var insertUserSql = @"
                INSERT INTO Users_Raj (FullName, Email, PasswordHash, RoleId, PhoneNumber, DepartmentId, IsActive, CreatedAt)
                OUTPUT INSERTED.UserId
                VALUES (@FullName, @Email, @PasswordHash, @RoleId, @PhoneNumber, @DepartmentId, 1, GETDATE());";

            using (var userCmd = new SqlCommand(insertUserSql, connection))
            {
                userCmd.Parameters.AddWithValue("@FullName", dto.FullName.Trim());
                userCmd.Parameters.AddWithValue("@Email", dto.Email.Trim().ToLower());
                userCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                userCmd.Parameters.AddWithValue("@RoleId", roleId);
                userCmd.Parameters.AddWithValue("@PhoneNumber", (object?)dto.PhoneNumber?.Trim() ?? DBNull.Value);
                userCmd.Parameters.AddWithValue("@DepartmentId", departmentIdParam);

                newUserId = (int)await userCmd.ExecuteScalarAsync()!;
            }

            // 6. If user is a Patient, create their patient record in Patients_Raj
            if (roleId == 3)
            {
                // Parse prefix and names from FullName
                string prefix = "Mr.";
                string firstName = dto.FullName.Trim();
                string lastName = "Patient";

                var parts = dto.FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && (parts[0].Equals("Mr.", StringComparison.OrdinalIgnoreCase) ||
                                         parts[0].Equals("Mrs.", StringComparison.OrdinalIgnoreCase) ||
                                         parts[0].Equals("Ms.", StringComparison.OrdinalIgnoreCase) ||
                                         parts[0].Equals("Dr.", StringComparison.OrdinalIgnoreCase)))
                {
                    prefix = parts[0];
                    if (parts.Length > 1) firstName = parts[1];
                    if (parts.Length > 2) lastName = string.Join(" ", parts.Skip(2));
                }
                else if (parts.Length > 1)
                {
                    firstName = parts[0];
                    lastName = string.Join(" ", parts.Skip(1));
                }

                DateTime dob = DateTime.TryParse(dto.DateOfBirth, out var parsedDob) ? parsedDob : DateTime.Today.AddYears(-(dto.Age ?? 25));
                int age = dto.Age ?? 25;
                string adhar = !string.IsNullOrWhiteSpace(dto.AdharNumber) ? dto.AdharNumber : $"{RandomNumberGenerator.GetInt32(1000, 9999)}-{RandomNumberGenerator.GetInt32(1000, 9999)}-{RandomNumberGenerator.GetInt32(1000, 9999)}";

                var insertPatientSql = @"
                    INSERT INTO Patients_Raj (UserId, Prefix, FirstName, LastName, Gender, PhoneNumber, Email,
                                             AnyMedicalHistory, Problem, AdharNumber, DateOfBirth, Age, IsDeleted, CreatedAt, CreatedBy)
                    VALUES (@UserId, @Prefix, @FirstName, @LastName, @Gender, @PhoneNumber, @Email,
                            @History, @Problem, @Adhar, @Dob, @Age, 0, GETDATE(), 'Admin');";

                using (var patCmd = new SqlCommand(insertPatientSql, connection))
                {
                    patCmd.Parameters.AddWithValue("@UserId", newUserId);
                    patCmd.Parameters.AddWithValue("@Prefix", prefix);
                    patCmd.Parameters.AddWithValue("@FirstName", firstName);
                    patCmd.Parameters.AddWithValue("@LastName", lastName);
                    patCmd.Parameters.AddWithValue("@Gender", (object?)dto.Gender ?? "Male");
                    patCmd.Parameters.AddWithValue("@PhoneNumber", dto.PhoneNumber?.Trim() ?? "000-000-0000");
                    patCmd.Parameters.AddWithValue("@Email", dto.Email.Trim().ToLower());
                    patCmd.Parameters.AddWithValue("@History", (object?)dto.AnyMedicalHistory ?? DBNull.Value);
                    patCmd.Parameters.AddWithValue("@Problem", (object?)dto.Problem ?? DBNull.Value);
                    patCmd.Parameters.AddWithValue("@Adhar", adhar);
                    patCmd.Parameters.AddWithValue("@Dob", dob);
                    patCmd.Parameters.AddWithValue("@Age", age);

                    await patCmd.ExecuteNonQueryAsync();
                }
            }

            return Ok(new { success = true, message = $"{dto.Role} user created successfully!", userId = newUserId });
        }

        // =========================================================================
        // 3. DELETE / DEACTIVATE USER
        // =========================================================================
        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var connectionString = GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Check if user exists
            var checkSql = "SELECT RoleId, Email FROM Users_Raj WHERE UserId = @UserId";
            int roleId = 0;
            string email = "";
            using (var checkCmd = new SqlCommand(checkSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@UserId", userId);
                using var reader = await checkCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    roleId = reader.GetInt32(0);
                    email = reader.GetString(1);
                }
                else
                {
                    return NotFound(new { message = "User not found." });
                }
            }

            // Soft-deactivate user in Users_Raj
            var deactUserSql = "UPDATE Users_Raj SET IsActive = 0, UpdatedAt = GETDATE() WHERE UserId = @UserId";
            using (var deactCmd = new SqlCommand(deactUserSql, connection))
            {
                deactCmd.Parameters.AddWithValue("@UserId", userId);
                await deactCmd.ExecuteNonQueryAsync();
            }

            // If user is a patient, also mark IsDeleted = 1 in Patients_Raj
            var deactPatSql = "UPDATE Patients_Raj SET IsDeleted = 1, DeletedDate = GETDATE(), DeletedBy = 'Admin' WHERE UserId = @UserId OR Email = @Email";
            using (var deactPatCmd = new SqlCommand(deactPatSql, connection))
            {
                deactPatCmd.Parameters.AddWithValue("@UserId", userId);
                deactPatCmd.Parameters.AddWithValue("@Email", email);
                await deactPatCmd.ExecuteNonQueryAsync();
            }

            return Ok(new { success = true, message = "User has been successfully deleted/deactivated from the system." });
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
