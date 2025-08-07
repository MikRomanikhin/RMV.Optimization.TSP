
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Hack for abstract async method declaration
/// </summary>
public interface ITspAsync
{
	//abstract Task<TspResult> RunAsync();
	Task<TspResult> RunAsync( CancellationToken? token = null );
}
