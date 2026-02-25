using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace EventEaseApp2.Services
{
    /// <summary>
    /// Service for persisting data to browser LocalStorage.
    /// Enables session data to survive page refreshes and browser restarts.
    /// </summary>
    public class LocalStorageService
    {
        private readonly IJSRuntime _jsRuntime;

        public LocalStorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <summary>
        /// Saves an object to LocalStorage as JSON.
        /// </summary>
        public async Task SetItemAsync<T>(string key, T value)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving to localStorage: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves an object from LocalStorage and deserializes it.
        /// </summary>
        public async Task<T?> GetItemAsync<T>(string key)
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                
                if (string.IsNullOrEmpty(json))
                    return default;

                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading from localStorage: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Removes an item from LocalStorage.
        /// </summary>
        public async Task RemoveItemAsync(string key)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing from localStorage: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears all items from LocalStorage.
        /// </summary>
        public async Task ClearAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.clear");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing localStorage: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a key exists in LocalStorage.
        /// </summary>
        public async Task<bool> ContainsKeyAsync(string key)
        {
            try
            {
                var value = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                return !string.IsNullOrEmpty(value);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Data transfer object for persisting user session to storage.
    /// </summary>
    public class UserSessionData
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime SessionStartTime { get; set; }
        public int RegisteredEventCount { get; set; }
    }
}
