using GpsUtil.Location;
using System.Collections.Concurrent;
using TripPricer;
            

namespace TourGuide.Users;

public class User
{
    public Guid UserId { get; }
    public string UserName { get; }
    public string PhoneNumber { get; set; }
    public string EmailAddress { get; set; }
    public DateTime LatestLocationTimestamp { get; set; }

    private readonly ConcurrentBag<VisitedLocation> _visitedLocations = new();
    private readonly HashSet<Guid> _rewardedAttractionIds = new();
    public IReadOnlyCollection<VisitedLocation> VisitedLocations => _visitedLocations;

    private readonly ConcurrentBag<UserReward> _userRewards = new();
    public IReadOnlyCollection<UserReward> UserRewards => _userRewards;

    public UserPreferences UserPreferences { get; set; } = new UserPreferences();
    public List<Provider> TripDeals { get; set; } = new List<Provider>();

    public User(Guid userId, string userName, string phoneNumber, string emailAddress)
    {
        UserId = userId;
        UserName = userName;
        PhoneNumber = phoneNumber;
        EmailAddress = emailAddress;
    }

    public void AddToVisitedLocations(VisitedLocation visitedLocation)
    {
        _visitedLocations.Add(visitedLocation);
    }

    public void ClearVisitedLocations()
    {
        _visitedLocations.Clear();
    }

    public void AddUserReward(UserReward userReward)
    {
        if (_rewardedAttractionIds.Add(userReward.Attraction.AttractionId))
        {
            _userRewards.Add(userReward);
        }
    }

    public VisitedLocation GetLastVisitedLocation()
    {
        return _visitedLocations.Last();
    }
}
