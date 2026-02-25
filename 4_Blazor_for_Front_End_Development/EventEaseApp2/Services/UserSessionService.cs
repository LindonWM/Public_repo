using System;
using System.Threading.Tasks;

namespace EventEaseApp2.Services
{
    public class UserSessionService
    {
        private readonly LocalStorageService _localStorageService;
        private string _fullName = string.Empty;
        private string _email = string.Empty;
        private string _phone = string.Empty;
        private DateTime _sessionStartTime = DateTime.MinValue;
        private bool _isLoggedIn = false;
        private const string StorageKey = "userSession";

        public event Action? OnSessionChanged;

        public UserSessionService(LocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
        }

        public string FullName { get => _fullName; set => _fullName = value; }

        public string Email { get => _email; set => _email = value; }

        public string Phone { get => _phone; set => _phone = value; }

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            private set => _isLoggedIn = value;
        }

        public DateTime SessionStartTime
        {
            get => _sessionStartTime;
            private set => _sessionStartTime = value;
        }

        public int RegisteredEventCount { get; set; } = 0;

        /// <summary>
        /// Initializes a new user session with provided information and saves to localStorage.
        /// </summary>
        public async Task StartSessionAsync(string fullName, string email, string phone = "")
        {
            _fullName = fullName;
            _email = email;
            _phone = phone;
            _sessionStartTime = DateTime.Now;
            IsLoggedIn = true;
            
            await SaveSessionToStorageAsync();
            NotifyStateChanged();
        }

        /// <summary>
        /// Initializes session without async (for backward compatibility).
        /// </summary>
        public void StartSession(string fullName, string email, string phone = "")
        {
            _fullName = fullName;
            _email = email;
            _phone = phone;
            _sessionStartTime = DateTime.Now;
            IsLoggedIn = true;
            
            SaveSessionToStorageAsync().ConfigureAwait(false);
            NotifyStateChanged();
        }

        /// <summary>
        /// Ends the current user session and clears all data.
        /// </summary>
        public async Task EndSessionAsync()
        {
            _fullName = string.Empty;
            _email = string.Empty;
            _phone = string.Empty;
            _sessionStartTime = DateTime.MinValue;
            IsLoggedIn = false;
            RegisteredEventCount = 0;
            
            await _localStorageService.RemoveItemAsync(StorageKey);
            NotifyStateChanged();
        }

        /// <summary>
        /// Ends session without async (for backward compatibility).
        /// </summary>
        public void EndSession()
        {
            _fullName = string.Empty;
            _email = string.Empty;
            _phone = string.Empty;
            _sessionStartTime = DateTime.MinValue;
            IsLoggedIn = false;
            RegisteredEventCount = 0;
            
            _ = _localStorageService.RemoveItemAsync(StorageKey);
            NotifyStateChanged();
        }

        /// <summary>
        /// Gets user session summary information.
        /// </summary>
        public SessionInfo GetSessionInfo()
        {
            return new SessionInfo
            {
                FullName = _fullName,
                Email = _email,
                Phone = _phone,
                IsLoggedIn = IsLoggedIn,
                SessionStartTime = SessionStartTime,
                SessionDurationMinutes = IsLoggedIn ? (int)(DateTime.Now - SessionStartTime).TotalMinutes : 0,
                RegisteredEventCount = RegisteredEventCount
            };
        }

        /// <summary>
        /// Increments the registered event counter and saves to storage.
        /// </summary>
        public void IncrementRegisteredEventCount()
        {
            RegisteredEventCount++;
            _ = SaveSessionToStorageAsync();
            NotifyStateChanged();
        }

        /// <summary>
        /// Resets the registered event counter.
        /// </summary>
        public void ResetEventCounter()
        {
            RegisteredEventCount = 0;
            NotifyStateChanged();
        }

        /// <summary>
        /// Checks if user is currently in an active session.
        /// </summary>
        public bool IsSessionActive()
        {
            return IsLoggedIn && !string.IsNullOrEmpty(_email);
        }

        /// <summary>
        /// Loads user session from localStorage if it exists.
        /// </summary>
        public async Task LoadSessionFromStorageAsync()
        {
            try
            {
                var sessionData = await _localStorageService.GetItemAsync<UserSessionData>(StorageKey);
                
                if (sessionData != null)
                {
                    _fullName = sessionData.FullName;
                    _email = sessionData.Email;
                    _phone = sessionData.Phone;
                    _sessionStartTime = sessionData.SessionStartTime;
                    RegisteredEventCount = sessionData.RegisteredEventCount;
                    IsLoggedIn = true;
                    NotifyStateChanged();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading session from storage: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves current session to localStorage.
        /// </summary>
        private async Task SaveSessionToStorageAsync()
        {
            try
            {
                if (IsLoggedIn)
                {
                    var sessionData = new UserSessionData
                    {
                        FullName = _fullName,
                        Email = _email,
                        Phone = _phone,
                        SessionStartTime = _sessionStartTime,
                        RegisteredEventCount = RegisteredEventCount
                    };

                    await _localStorageService.SetItemAsync(StorageKey, sessionData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving session to storage: {ex.Message}");
            }
        }

        private void NotifyStateChanged()
        {
            OnSessionChanged?.Invoke();
        }
    }

    /// <summary>
    /// Represents a snapshot of user session information.
    /// </summary>
    public class SessionInfo
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsLoggedIn { get; set; }
        public DateTime SessionStartTime { get; set; }
        public int SessionDurationMinutes { get; set; }
        public int RegisteredEventCount { get; set; }
    }
}
