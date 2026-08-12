using GpsUtil.Location;
using System.Collections.Concurrent;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Users;

namespace TourGuide.Services;

public class RewardsService : IRewardsService
{
    private const double StatuteMilesPerNauticalMile = 1.15077945;
    private readonly int _defaultProximityBuffer = 10;
    private int _proximityBuffer;
    private readonly int _attractionProximityRange = 200;
    private readonly IGpsUtil _gpsUtil;
    private readonly IRewardCentral _rewardsCentral;
    //private static int count = 0;
    //cache pour optimisation calcul
    private readonly ConcurrentDictionary<(double lat1, double lon1, double lat2, double lon2), double> _distanceCache
    = new();

    public RewardsService(IGpsUtil gpsUtil, IRewardCentral rewardCentral)
    {
        _gpsUtil = gpsUtil;
        _rewardsCentral =rewardCentral;
        _proximityBuffer = _defaultProximityBuffer;
    }

    public void SetProximityBuffer(int proximityBuffer)
    {
        _proximityBuffer = proximityBuffer;
    }

    public void SetDefaultProximityBuffer()
    {
        _proximityBuffer = _defaultProximityBuffer;
    }

    public async Task  CalculateRewards(User user)
    {
       await  CalculateRewards(user, await _gpsUtil.GetAttractions());
    }
    public async Task CalculateRewards(User user, List<Attraction> attractions)
    {
        var tasks = attractions
            .Select(async attraction =>
            {
                foreach (var visitedLocation in user.VisitedLocations)
                {
                    if (NearAttraction(visitedLocation, attraction))
                    {
                        int points = await GetRewardPoints(attraction, user);
                        user.AddUserReward(new UserReward(visitedLocation, attraction, points));
                        break;
                    }
                }
            })
            .ToList();

        await Task.WhenAll(tasks);
    }
    public bool IsWithinAttractionProximity(Attraction attraction, Locations location)
    {
       // Console.WriteLine(GetDistance(attraction, location));
        return GetDistance(attraction, location) <= _attractionProximityRange;
    }

    private bool NearAttraction(VisitedLocation visitedLocation, Attraction attraction)
    {
        return GetDistance(attraction, visitedLocation.Location) <= _proximityBuffer;
    }

    private Task<int> GetRewardPoints(Attraction attraction, User user)
    {
        return _rewardsCentral.GetAttractionRewardPoints(attraction.AttractionId, user.UserId);
    }

    public double GetDistance(Locations loc1, Locations loc2)
    {
        //Controle si présent dasn le cache
        var key = (loc1.Latitude, loc1.Longitude, loc2.Latitude, loc2.Longitude);
        if (_distanceCache.TryGetValue(key, out double cachedDistance))
        {
            return cachedDistance;
        }
        //Mise encache
        double lat1 = Math.PI * loc1.Latitude / 180.0;
        double lon1 = Math.PI * loc1.Longitude / 180.0;
        double lat2 = Math.PI * loc2.Latitude / 180.0;
        double lon2 = Math.PI * loc2.Longitude / 180.0;

        double angle = Math.Acos(Math.Sin(lat1) * Math.Sin(lat2)
                                + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2));

        double nauticalMiles = 60.0 * angle * 180.0 / Math.PI;
        //sauvegarde en  cache
        double distance = StatuteMilesPerNauticalMile * nauticalMiles;
        _distanceCache.TryAdd(key, distance);
        return distance;
    }
}
