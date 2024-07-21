using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace FunctionApp
{
    public class Student
    {
        public int student_id { get; set; }
        public string? student_name { get; set; }
        public int student_age { get; set; }
        public string? student_addr { get; set; }
        public double student_percent { get; set; }
        public string? student_qual { get; set; }
        public int student_year_passed { get; set; }
    }

    public class Function1
    {
        private readonly ILogger<Function1> _logger;
        //private readonly string connectionString = "Server=testmysqlflex.mysql.database.azure.com;Database=test;User=mysqladmin;Password=Password@123;SslMode=Required;";

        #pragma warning disable CS8601 // Possible null reference assignment.
        private readonly string connectionString = Environment.GetEnvironmentVariable("MySQLConnectionString", EnvironmentVariableTarget.Process);
        #pragma warning restore CS8601 // Possible null reference assignment.

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("AddStudent")]
        public async Task<IActionResult> AddStudentAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("Adding a new student.");

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var student = JsonSerializer.Deserialize<Student>(requestBody);

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "INSERT INTO students (student_name, student_age, student_addr, student_percent, student_qual, student_year_passed) VALUES (@student_name, @student_age, @student_addr, @student_percent, @student_qual, @student_year_passed)";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    MySqlParameter mySqlParameter = _ = command.Parameters.AddWithValue("@student_name", value: student.student_name);
                    command.Parameters.AddWithValue("@student_age", student.student_age);
                    command.Parameters.AddWithValue("@student_addr", student.student_addr);
                    command.Parameters.AddWithValue("@student_percent", student.student_percent);
                    command.Parameters.AddWithValue("@student_qual", student.student_qual);
                    command.Parameters.AddWithValue("@student_year_passed", student.student_year_passed);

                    await command.ExecuteNonQueryAsync();

                    return new OkObjectResult("Student added successfully!");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding student: {ex.Message}");
                return new ObjectResult($"Error adding student: {ex.Message}") { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        [Function("Getstudents")]
        public async Task<IActionResult> GetstudentsAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            _logger.LogInformation("Retrieving students.");

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT * FROM students";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        var students = new List<Student>();
                        while (reader.Read())
                        {
                            students.Add(new Student
                            {
                                student_id = reader.GetInt32("student_id"),
                                student_name = reader.GetString("student_name"),
                                student_age = reader.GetInt32("student_age"),
                                student_addr = reader.GetString("student_addr"),
                                student_percent = reader.GetDouble("student_percent"),
                                student_qual = reader.GetString("student_qual"),
                                student_year_passed = reader.GetInt32("student_year_passed")
                            });
                        }

                        return new OkObjectResult(students);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving students: {ex.Message}");
                return new ObjectResult($"Error retrieving students: {ex.Message}") { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        [Function("UpdateStudent")]
        public async Task<IActionResult> UpdateStudentAsync([HttpTrigger(AuthorizationLevel.Function, "put")] HttpRequest req)
        {
            _logger.LogInformation("Updating student.");

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var student = JsonSerializer.Deserialize<Student>(requestBody);

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "UPDATE students SET student_name = @student_name, student_age = @student_age, student_addr = @student_addr, student_percent = @student_percent, student_qual = @student_qual, student_year_passed = @student_year_passed WHERE student_id = @student_id";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@student_name", student.student_name);
                    command.Parameters.AddWithValue("@student_age", student.student_age);
                    command.Parameters.AddWithValue("@student_addr", student.student_addr);
                    command.Parameters.AddWithValue("@student_percent", student.student_percent);
                    command.Parameters.AddWithValue("@student_qual", student.student_qual);
                    command.Parameters.AddWithValue("@student_year_passed", student.student_year_passed);
                    command.Parameters.AddWithValue("@student_id", student.student_id);

                    await command.ExecuteNonQueryAsync();

                    return new OkObjectResult("Student updated successfully!");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating student: {ex.Message}");
                return new ObjectResult($"Error updating student: {ex.Message}") { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        [Function("DeleteStudent")]
        public async Task<IActionResult> DeleteStudentAsync(
    [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "students/{id}")] HttpRequest req,
    int id)
        {
            _logger.LogInformation($"Deleting student with ID: {id}");

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "DELETE FROM students WHERE student_id = @student_id";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@student_id", id);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        return new OkObjectResult("Student deleted successfully!");
                    }
                    else
                    {
                        return new NotFoundObjectResult("Student not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting student: {ex.Message}");
                return new ObjectResult($"Error deleting student: {ex.Message}") { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

    }
}
