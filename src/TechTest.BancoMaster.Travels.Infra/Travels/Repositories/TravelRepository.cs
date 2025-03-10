﻿using Microsoft.Extensions.Logging;
using TechTest.BancoMaster.Travels.Domain.Travels;
using TechTest.BancoMaster.Travels.Domain.Travels.Repositories;
using TechTest.BancoMaster.Travels.Infra.Shared;

namespace TechTest.BancoMaster.Travels.Infra.Travels.Repositories;

public class TravelRepository : BaseInMemoryRepository<Travel, string, TravelRepository>, ITravelRepository
{
    private Dictionary<string, Travel> _travels;

    public TravelRepository(ILogger<TravelRepository> logger, Dictionary<string, Travel> travels) : base(logger)
    {
        _travels = travels ?? throw new ArgumentNullException(nameof(travels));
    }

    protected override Dictionary<string, Travel> Data => _travels;

    protected override Travel NullValue => Travel.Null;

    public async Task<IEnumerable<Travel>> GetConnectionLocations(Location startingPoint, Location destination)
    {
        var firstQuery = await GetWhereAsync(x => x.Id.Contains(startingPoint) || x.Id.Contains(destination));
        var locations = firstQuery.SelectMany(x => new[] { x.Connection.StartingPoint, x.Connection.Destination }).ToHashSet();

        var queried = await GetWhereAsync(x => locations.Contains(x.Connection.StartingPoint) || locations.Contains(x.Connection.Destination));

        return queried;
    }

    public async Task<IEnumerable<Travel>> GetTravelsAsync(int offset = 0, int size = 100)
    {
        return Data.Skip(offset * size).Take(size).Select(x => x.Value);
    }
}

