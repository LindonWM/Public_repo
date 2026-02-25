using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventEaseApp2.Services
{

public record EventInfo(int Id, string Name, DateTime Date, string Location);

public class EventService
{
    private readonly LocalStorageService _localStorageService;
    private List<EventInfo> _events = new();
    private int _nextId = 19;
    private const int PageSize = 10;
    private const string StorageKey = "events";

    public EventService(LocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
        InitializeMockEvents();
    }

    /// <summary>
    /// Initializes with mock events (default data).
    /// </summary>
    private void InitializeMockEvents()
    {
        _events = new()
        {
            new EventInfo(1, "Tech Meetup", DateTime.Today.AddDays(7), "Warsaw"),
            new EventInfo(2, "Startup Pitch Night", DateTime.Today.AddDays(14), "Krakow"),
            new EventInfo(3, "Design Workshop", DateTime.Today.AddDays(21), "Gdansk"),
            new EventInfo(4, "AI Conference", DateTime.Today.AddDays(28), "Wroclaw"),
            new EventInfo(5, "Product Leadership Forum", DateTime.Today.AddDays(35), "Poznan"),
            new EventInfo(6, "Cybersecurity Summit", DateTime.Today.AddDays(42), "Lodz"),
            new EventInfo(7, "Health Tech Expo", DateTime.Today.AddDays(49), "Katowice"),
            new EventInfo(8, "Fintech Meetup", DateTime.Today.AddDays(56), "Warsaw"),
            new EventInfo(9, "Green Energy Talk", DateTime.Today.AddDays(63), "Gdynia"),
            new EventInfo(10, "UX Research Day", DateTime.Today.AddDays(70), "Krakow"),
            new EventInfo(11, "Cloud Architecture Bootcamp", DateTime.Today.AddDays(77), "Gdansk"),
            new EventInfo(12, "DevOps Live", DateTime.Today.AddDays(84), "Wroclaw"),
            new EventInfo(13, "Startup Demo Night", DateTime.Today.AddDays(91), "Poznan"),
            new EventInfo(14, "Open Source Hackathon", DateTime.Today.AddDays(98), "Lublin"),
            new EventInfo(15, "Data Science Roundtable", DateTime.Today.AddDays(105), "Warsaw"),
            new EventInfo(16, "Mobile Dev Summit", DateTime.Today.AddDays(112), "Szczecin"),
            new EventInfo(17, "Game Dev Meetup", DateTime.Today.AddDays(119), "Rzeszow"),
            new EventInfo(18, "Agile Coaching Clinic", DateTime.Today.AddDays(126), "Bialystok")
        };
    }

    /// <summary>
    /// Loads events from localStorage. If none exist, uses mock data.
    /// </summary>
    public async Task LoadFromStorageAsync()
    {
        try
        {
            var storedEvents = await _localStorageService.GetItemAsync<List<EventInfo>>(StorageKey);
            
            if (storedEvents?.Any() == true)
            {
                _events = storedEvents;
                _nextId = _events.Max(e => e.Id) + 1;
            }
            else
            {
                // Use mock data and save to storage
                await SaveToStorageAsync();
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading events from storage: {ex.Message}");
            // Fall back to mock data
        }
    }

    public List<EventInfo> GetEvents() => _events.OrderBy(e => e.Date).ToList();

    public List<EventInfo> GetEventsPaged(int pageNumber) 
    {
        if (pageNumber < 1) pageNumber = 1;
        return _events.OrderBy(e => e.Date)
                      .Skip((pageNumber - 1) * PageSize)
                      .Take(PageSize)
                      .ToList();
    }

    public int GetEventCount() => _events.Count;

    public int GetPageCount() => (int)Math.Ceiling((double)_events.Count / PageSize);

    public EventInfo? GetEventById(int id) => _events.FirstOrDefault(e => e.Id == id);

    public void AddEvent(string name, DateTime date, string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(location, nameof(location));
        
        if (date < DateTime.Now)
            throw new ArgumentException("Event date cannot be in the past", nameof(date));

        var newEvent = new EventInfo(_nextId++, name, date, location);
        _events.Add(newEvent);
        
        SaveToStorageAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Saves events to localStorage.
    /// </summary>
    private async Task SaveToStorageAsync()
    {
        try
        {
            await _localStorageService.SetItemAsync(StorageKey, _events);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving events to storage: {ex.Message}");
        }
    }
}
}
