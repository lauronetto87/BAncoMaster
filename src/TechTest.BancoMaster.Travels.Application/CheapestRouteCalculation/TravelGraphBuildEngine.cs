using Awarean.Sdk.Result;
using Awarean.Sdk.ValueObjects;
using Microsoft.Extensions.Logging;
using TechTest.BancoMaster.Travels.Domain.CheapestRouteCalculation;
using TechTest.BancoMaster.Travels.Domain.Extensions;
using TechTest.BancoMaster.Travels.Domain.Structures;
using TechTest.BancoMaster.Travels.Domain.Travels;

namespace TechTest.BancoMaster.Travels.Application.CheapestRouteCalculation;

public class TravelGraphBuildEngine : ITravelGraphBuildEngine
{
    private readonly IGraphBuilder _graphBuilder;
    private readonly INodeBuilder _nodeBuilder;
    private readonly ILogger<TravelGraphBuildEngine> _logger;

    public TravelGraphBuildEngine(IGraphBuilder graphBuilder, INodeBuilder nodeBuilder, ILogger<TravelGraphBuildEngine> logger)
    {
        _graphBuilder = graphBuilder ?? throw new ArgumentNullException(nameof(graphBuilder));
        _nodeBuilder = nodeBuilder ?? throw new ArgumentNullException(nameof(nodeBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Result<DirectedGraph> BuildGraph(IEnumerable<Travel> travelList)
    {
        try
        {
            var graph = MakeGraphFromTravels(travelList);

            return Result<DirectedGraph>.Success(graph);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private DirectedGraph MakeGraphFromTravels(IEnumerable<Travel> travels)
    {
        var locations = GetAllLocations(travels);
        _logger.LogInformation($"Found a total of {travels.Count()} - {travels.FormatStringFor(x => x.Connection.FromTo)}");

        _logger.LogInformation($"Building Graph for {locations.ToFormatString()}");
        foreach (var location in locations)
        {
            var nodeLinks = travels
                .Where(x => x.Connection.StartingPoint == location)
                .Select(x => (x.Connection.Destination, x.Amount));

            _nodeBuilder.Create(location);

            var node = LinkAndBuildNode(nodeLinks);
            _graphBuilder.AddNode(node);
        }

        var graph = _graphBuilder.Build();

        return graph;
    }

    private Node LinkAndBuildNode(IEnumerable<(Location destination, Money weight)> nodeLinks)
    {
        foreach (var (destination, weight) in nodeLinks)
            _nodeBuilder.LinkTo(destination, weight);

        var node = _nodeBuilder.Build();
        _logger.LogInformation($"Graph's node Build for location: {node.Name} containing {node.Links.Count} links to other places - {node.Links.ToFormatString()}");

        return node;
    }

    private static HashSet<Location> GetAllLocations(IEnumerable<Travel> travels)
    {
        var locations = new HashSet<Location>();
        var connections = travels.Select(x => x.Connection);

        foreach (var connection in connections)
        {
            locations.Add(connection.StartingPoint);
            locations.Add(connection.Destination);
        }

        return locations;
    }

    private Result<DirectedGraph> HandleException(Exception ex)
    {
        var message = $"Failed trying to build graph for travel route, exception happened : {ex}";
        _logger.LogInformation(message);
        return Result<DirectedGraph>.Fail("GRAPH_BUILDING_ERROR", message);
    }
}
