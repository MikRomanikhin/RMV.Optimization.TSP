using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// K-Nearest Neighbour TSP algorithm
/// </summary>
/// <param name="map">TSP map</param>
public class K_NearestSearch( TspMap map ) : TspAlgorithmBase( map ), ITspAsync
{
	BeamSettings settings;
	readonly TspMap Map = map;

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<BeamSettings>( "beam" );
	}


	/// <summary>
	/// Applies NN algorithms to each node
	/// </summary>	
	public async Task<TspResult> RunAsync(CancellationToken token )
	{
		base.timer.Start();
		TspResult best = null;

		await Task.Run( () => 
		{
			best = RunKNearest( new TspResult( 0, [ 0 ] ) );

			base.Draw( best.Tour, 0, best.Path );					
		} );

		base.timer.Stop();

		return best;
	}

	/// <summary>
	/// K-Nearest Neighbour algorithm
	/// </summary>
	/// <param name="start">starting node</param>	
	protected TspResult RunKNearest( TspResult result )
	{
		var path = result.Path; // Copy the current path
		double tour = result.Tour; // Current tour length

		var available = Enumerable.Range( 0, this.Cities ).Except( path ).ToList();

		while( available.Any() )
		{
			var nearest = available.OrderBy( city => this.Map[ path[ ^1 ], city ].Weight ).Take( settings.Size );

			foreach( int city in nearest )
			{
				tour += this.Map[ path[ ^1 ], city ].Weight;

				path.Add( city );

				available.Remove( city );

				result = RunKNearest( new TspResult( tour, path ) ); // Recursive call to continue building the path

				if( result.Path.Count == this.Cities ) return result; // If we have a complete path, return it				
			}
		}

		tour += this.Map[ path[ 0 ], path[ ^1 ] ].Weight;

		return new TspResult( tour, path );
	}
}	
