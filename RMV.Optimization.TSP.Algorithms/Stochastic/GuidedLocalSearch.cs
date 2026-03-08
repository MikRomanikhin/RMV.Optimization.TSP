using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Guided Local Search for TSP
/// </summary>
public class GuidedLocalSearch( TspMap map ) : TspAlgorithmBase( map )
{	
	double lambda = 1;	

	/// <summary>
	/// Configures the algorithm
	/// </summary>	
	protected override void Configure()
	{		
		base.settings = ConfigManager.GetSection<GlsSettings>( "gls" ) ?? throw new ArgumentNullException( nameof( settings ) );
		var glsSettings = ( GlsSettings )base.settings;

		lambda = 0.3 * glsSettings.Optima / base.Cities;
	}

	/// <summary>
	/// Performs a single optimization epoch using local search and feature-based penalties.
	/// </summary>
	protected override TspResult RunEpoch( TspResult best )
	{
		var result = LocalSearch( best );

		UpdatePenalties( result.Path );

		return result;
	}

	/// <summary>
	/// Performs a local search optimization on the given TSP result.
	/// </summary>
	/// <param name="best">The current best TSP result.</param>
	/// <returns>The optimized TSP result.</returns>
	TspResult LocalSearch( TspResult best )
	{
		double oldCost = UpdateTotalCost( best.Path );

		int count = 0;
		int noChanges = 0;

		// Pre-allocate a single array/list to swap into, avoiding continuous Gen0 allocations
		var workingPath = new List<int>( best.Path.Count );
		workingPath.AddRange( best.Path );

		while( true )
		{
			pauseEvent.Wait(); // Pause/resume support

			// 1. Always reset our working path back to the currently known best
			workingPath.Clear();
			workingPath.AddRange( best.Path );

			// 2. Perform the swap attempt
			(Action accept, double delta) = base.Swap( workingPath );

			if( delta < 0 )
			{
				accept!(); // Acknowledge the swap in the base tracking (if any)

				// 3. Evaluate the penalized cost of this new layout
				double cost = UpdateTotalCost( workingPath );

				if( cost < oldCost )
				{
					// Update best. We copy values explicitly to reuse memory
					best.Path.Clear();
					best.Path.AddRange( workingPath );
					best.Tour += delta;

					oldCost = cost;
					noChanges = 0;
					base.Draw( best.Tour, count, best.Path );
				}
			}

			if( ++noChanges > 200 ) break;
		}

		return best;
	}
	
	/// <summary>
	/// Calculates the total cost of traversing a specified path, including both weights and penalties for each segment.
	/// </summary>
	/// <remarks>
	/// Method assumes that each pair of consecutive nodes in the path corresponds to a valid edge in the underlying map. 
	/// The cost calculation includes a penalty term scaled by the current lambda value. 
	/// </remarks>
	/// <param name="path">A list of node indices representing the traversal path.</param>
	/// <returns>The total cost of the path, computed as the sum of weights and penalties.</returns>
	double UpdateTotalCost( List<int> path )
	{
		double cost = 0;

		for( int i = 0; i < path.Count; i++ )
		{
			int c1 = path[ i ];
			int c2 = i == path.Count - 1 ? path[ 0 ] : path[ i + 1 ];

			cost += base.map[ c1, c2 ].Weight + this.lambda * base.map[ c1, c2 ].Penalty;
		}

		return cost;
	}

	/// <summary>
	/// Updates the penalties for each edge in the given path based on their utility.
	/// </summary>
	/// <param name="path">Traversal Path</param>
	void UpdatePenalties( List<int> path )
	{
		double maxUtility = double.MinValue;

		// 1. First pass: find max utility (replaces allocating array and calling LINQ .Max())
		for( int i = 0; i < path.Count; i++ )
		{
			int c1 = path[ i ];
			int c2 = i == path.Count - 1 ? path[ 0 ] : path[ i + 1 ];

			double utility = base.map[ c1, c2 ].Weight / ( 1.0 + base.map[ c1, c2 ].Penalty );
			if( utility > maxUtility ) maxUtility = utility;
		}

		// 2. Second pass: update penalties where utility is close to max
		for( int i = 0; i < path.Count; i++ )
		{
			int c1 = path[ i ];
			int c2 = i == path.Count - 1 ? path[ 0 ] : path[ i + 1 ];

			double utility = base.map[ c1, c2 ].Weight / ( 1.0 + base.map[ c1, c2 ].Penalty );
			
			if( utility + MARGIN > maxUtility )
			{
				base.map[ c1, c2 ].Penalty += 1.0;
			}
		}
	}
	
}

/// <summary>
/// Configuration Settings
/// </summary>
class GlsSettings : TspConfigurationBase
{
	public double Optima { get; set; }	
}
