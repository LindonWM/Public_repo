using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventEaseApp2.Services
{
    /// <summary>
    /// Tracks user attendance and participation for events.
    /// Persists data to localStorage to survive browser refreshes.
    /// </summary>
    public class AttendanceTrackerService
    {
        private readonly LocalStorageService _localStorageService;
        private List<AttendanceRecord> _attendanceRecords = new();
        private const string StorageKey = "attendanceRecords";

        public event Action? OnAttendanceChanged;

        public AttendanceTrackerService(LocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
        }

        /// <summary>
        /// Loads attendance records from localStorage on app initialization.
        /// </summary>
        public async Task LoadFromStorageAsync()
        {
            try
            {
                var records = await _localStorageService.GetItemAsync<List<AttendanceRecord>>(StorageKey);
                
                if (records?.Any() == true)
                {
                    _attendanceRecords = records;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading attendance records from storage: {ex.Message}");
            }
        }

        /// <summary>
        /// Records a user's registration for an event.
        /// </summary>
        public void RegisterAttendance(int eventId, string fullName, string email, string? phone = null)
        {
            if (eventId <= 0)
                throw new ArgumentException("Event ID must be positive", nameof(eventId));

            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Full name is required", nameof(fullName));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required", nameof(email));

            var record = new AttendanceRecord
            {
                Id = Guid.NewGuid().ToString(),
                EventId = eventId,
                FullName = fullName,
                Email = email,
                Phone = phone ?? string.Empty,
                RegistrationDate = DateTime.Now,
                IsAttended = false
            };

            _attendanceRecords.Add(record);
            _ = SaveToStorageAsync();
            NotifyStateChanged();
        }

        /// <summary>
        /// Marks a user as attended for a specific event.
        /// </summary>
        public void MarkAsAttended(int eventId, string email)
        {
            var record = _attendanceRecords.FirstOrDefault(r => r.EventId == eventId && r.Email == email);
            if (record != null)
            {
                record.IsAttended = true;
                record.AttendanceDate = DateTime.Now;
                _ = SaveToStorageAsync();
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// Gets all attendance records for a specific event.
        /// </summary>
        public List<AttendanceRecord> GetEventAttendance(int eventId)
        {
            return _attendanceRecords.Where(r => r.EventId == eventId).OrderByDescending(r => r.RegistrationDate).ToList();
        }

        /// <summary>
        /// Gets attendance statistics for a specific event.
        /// </summary>
        public EventAttendanceStats GetEventStats(int eventId)
        {
            var eventRecords = _attendanceRecords.Where(r => r.EventId == eventId).ToList();
            return new EventAttendanceStats
            {
                EventId = eventId,
                TotalRegistered = eventRecords.Count,
                TotalAttended = eventRecords.Count(r => r.IsAttended),
                AttendanceRate = eventRecords.Count > 0 ? (double)eventRecords.Count(r => r.IsAttended) / eventRecords.Count * 100 : 0
            };
        }

        /// <summary>
        /// Gets all attendance records for a specific user.
        /// </summary>
        public List<AttendanceRecord> GetUserAttendance(string email)
        {
            return _attendanceRecords.Where(r => r.Email == email).OrderByDescending(r => r.RegistrationDate).ToList();
        }

        /// <summary>
        /// Gets user's attendance statistics.
        /// </summary>
        public UserAttendanceStats GetUserStats(string email)
        {
            var userRecords = _attendanceRecords.Where(r => r.Email == email).ToList();
            return new UserAttendanceStats
            {
                Email = email,
                TotalRegistered = userRecords.Count,
                TotalAttended = userRecords.Count(r => r.IsAttended),
                AttendancePercentage = userRecords.Count > 0 ? (double)userRecords.Count(r => r.IsAttended) / userRecords.Count * 100 : 0
            };
        }

        /// <summary>
        /// Checks if a user is already registered for an event.
        /// </summary>
        public bool IsUserRegisteredForEvent(int eventId, string email)
        {
            return _attendanceRecords.Any(r => r.EventId == eventId && r.Email == email);
        }

        /// <summary>
        /// Gets all attendance records.
        /// </summary>
        public List<AttendanceRecord> GetAllAttendanceRecords()
        {
            return _attendanceRecords.OrderByDescending(r => r.RegistrationDate).ToList();
        }

        /// <summary>
        /// Gets attendance records for a date range.
        /// </summary>
        public List<AttendanceRecord> GetAttendanceByDateRange(DateTime startDate, DateTime endDate)
        {
            return _attendanceRecords
                .Where(r => r.RegistrationDate >= startDate && r.RegistrationDate <= endDate)
                .OrderByDescending(r => r.RegistrationDate)
                .ToList();
        }

        /// <summary>
        /// Gets most popular events by registration count.
        /// </summary>
        public List<EventRegistrationStats> GetMostPopularEvents(int topCount = 5)
        {
            return _attendanceRecords
                .GroupBy(r => r.EventId)
                .Select(g => new EventRegistrationStats
                {
                    EventId = g.Key,
                    RegistrationCount = g.Count(),
                    AttendeeNames = string.Join(", ", g.Select(r => r.FullName).Distinct())
                })
                .OrderByDescending(e => e.RegistrationCount)
                .Take(topCount)
                .ToList();
        }

        /// <summary>
        /// Removes an attendance record.
        /// </summary>
        public bool RemoveAttendanceRecord(string recordId)
        {
            var record = _attendanceRecords.FirstOrDefault(r => r.Id == recordId);
            if (record != null)
            {
                _attendanceRecords.Remove(record);
                _ = SaveToStorageAsync();
                NotifyStateChanged();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets overall system statistics.
        /// </summary>
        public SystemAttendanceStats GetSystemStats()
        {
            var allRecords = _attendanceRecords.ToList();
            var uniqueUsers = allRecords.Select(r => r.Email).Distinct().Count();
            var uniqueEvents = allRecords.Select(r => r.EventId).Distinct().Count();

            return new SystemAttendanceStats
            {
                TotalRegistrations = allRecords.Count,
                UniqueUsers = uniqueUsers,
                UniqueEvents = uniqueEvents,
                TotalAttendances = allRecords.Count(r => r.IsAttended),
                OverallAttendanceRate = allRecords.Count > 0 ? (double)allRecords.Count(r => r.IsAttended) / allRecords.Count * 100 : 0
            };
        }

        /// <summary>
        /// Saves attendance records to localStorage.
        /// </summary>
        private async Task SaveToStorageAsync()
        {
            try
            {
                await _localStorageService.SetItemAsync(StorageKey, _attendanceRecords);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving attendance records to storage: {ex.Message}");
            }
        }

        private void NotifyStateChanged()
        {
            OnAttendanceChanged?.Invoke();
        }
    }

    /// <summary>
    /// Represents a single attendance record.
    /// </summary>
    public class AttendanceRecord
    {
        public string Id { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public bool IsAttended { get; set; }
        public DateTime? AttendanceDate { get; set; }
    }

    /// <summary>
    /// Statistics for an event's attendance.
    /// </summary>
    public class EventAttendanceStats
    {
        public int EventId { get; set; }
        public int TotalRegistered { get; set; }
        public int TotalAttended { get; set; }
        public double AttendanceRate { get; set; }
    }

    /// <summary>
    /// Statistics for a user's attendance.
    /// </summary>
    public class UserAttendanceStats
    {
        public string Email { get; set; } = string.Empty;
        public int TotalRegistered { get; set; }
        public int TotalAttended { get; set; }
        public double AttendancePercentage { get; set; }
    }

    /// <summary>
    /// Statistics for event registration popularity.
    /// </summary>
    public class EventRegistrationStats
    {
        public int EventId { get; set; }
        public int RegistrationCount { get; set; }
        public string AttendeeNames { get; set; } = string.Empty;
    }

    /// <summary>
    /// Overall system attendance statistics.
    /// </summary>
    public class SystemAttendanceStats
    {
        public int TotalRegistrations { get; set; }
        public int UniqueUsers { get; set; }
        public int UniqueEvents { get; set; }
        public int TotalAttendances { get; set; }
        public double OverallAttendanceRate { get; set; }
    }
}
