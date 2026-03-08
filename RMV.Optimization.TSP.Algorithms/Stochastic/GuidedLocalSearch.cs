using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Guided Local Search
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
	/// Local Search
	/// </summary>
	/// <summary>
	/// Local Search
	/// </summary>
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
	//TspResult LocalSearch( TspResult best )
	//{
	//	double oldCost = UpdateTotalCost( best.Path );

	//	int count = 0;
	//	int noChanges = 0;

	//	// Create a single working copy to avoid creating huge GC pressure inside the loop
	//	var workingCopy = best.Clone();

	//	while( true )
	//	{
	//		pauseEvent.Wait(); // Pause/resume support

	//		// Attempt a swap in-place on the working copy
	//		(Action accept, double delta) = base.Swap( workingCopy.Path );

	//		if( delta < 0 )
	//		{
	//			workingCopy.Tour += delta;
	//			accept!();
	//		}

	//		double cost = UpdateTotalCost( workingCopy.Path );

	//		if( cost < oldCost )
	//		{
	//			// Only clone when we actually find a verified better solution
	//			best = workingCopy.Clone();
	//			oldCost = cost;
	//			noChanges = 0;
	//			base.Draw( best.Tour, count, best.Path );
	//		}

	//		if( ++noChanges > 200 ) break;
	//	}

	//	return best;
	//}

	double UpdateTotalCost( IList<int> path )
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

	// Combined method to avoid allocating double[] utility array on every epoch
	void UpdatePenalties( IList<int> path )
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

			// Implicit MARGIN constant used as per the original codebase
			if( utility + MARGIN > maxUtility )
			{
				base.map[ c1, c2 ].Penalty += 1.0;
			}
		}
	}

	/// <summary>
	/// Performs a single optimization epoch using local search and feature-based penalties.
	/// </summary>
	/// <remarks>
	/// Method applies local search to the provided solution and updates feature penalties based on utility calculations. 
	/// </remarks>
	/// <param name="best">The current best solution to use as the starting point for the epoch. Must not be null.</param>
	/// <returns>A new TspResult representing the solution found after this epoch.</returns>
	//protected override TspResult RunEpoch( TspResult best )
	//{
	//	var result = LocalSearch( best );

	//	var utilities = GetFeatureUtilities( best.Path );

	//	UpdatePenalties( best.Path, utilities );

	//	return result;
	//}


	/// <summary>
	/// Local Search
	/// </summary>
	//TspResult LocalSearch( TspResult best )
	//{
	//	double oldCost = UpdateTotalCost( best.Path );

	//	int count = 0;
	//	int noChanges = 0;

	//	while( true )
	//	{			
	//		pauseEvent.Wait(); // Pause/resume support

	//		var result = Swap( best );	//result.UpdateTour( this.Map );

	//		double cost = UpdateTotalCost( result.Path );

	//		if( cost < oldCost )
	//		{
	//			best = result.Clone();
	//			oldCost = cost;
	//			noChanges = 0;
	//			base.Draw( best.Tour, count, best.Path );
	//		}			

	//		if( ++noChanges > 200 ) break;
	//	}

	//	return best;
	//}

	//TspResult Swap( TspResult result )
	//{
	//	var copy = result.Clone();

	//	(Action accept, double delta) = base.Swap( copy.Path );

	//	if( delta < 0 )
	//	{
	//		copy.Tour += delta;
	//		accept!();
	//	}

	//	return copy;
	//}

	//double UpdateTotalCost( IList<int> path )
	//{
	//	double cost = 0;

	//	for( int i = 0; i < path.Count; i++ )
	//	{
	//		int c1 = path[ i ];
	//		int c2 = i == path.Count - 1 ? path[ 0 ] : path[ i + 1 ];				

	//		cost += base.map[ c1, c2 ].Weight + this.lambda * base.map[ c1, c2 ].Penalty;
	//	}		

	//	return cost;
	//}

	//double[] GetFeatureUtilities( IList<int> path )
	//{
	//	var utilities = new double[ path.Count ];		

	//	for( int i = 0; i < path.Count; i++ )
	//	{
	//		int c1 = path[ i ];
	//		int c2 = i == path.Count - 1 ? path[ 0 ] : path[ i + 1 ];			

	//		utilities[ i ] = base.map[ c1, c2 ].Weight / ( 1.0 + base.map[ c1, c2 ].Penalty );
	//	}

	//	return utilities;
	//}

	//void UpdatePenalties( IList<int> path, double[] utilities )
	//{
	//	double max = utilities.Max();

	//	for( int i = 0; i < path.Count; i++ )
	//	{
	//		int c1 = path[ i ];
	//		int c2 = i == path.Count - 1 ? path[ 0 ] : path[ i + 1 ];			

	//		if( utilities[ i ] + MARGIN > max ) base.map[ c1, c2 ].Penalty += 1.0;
	//	}
	//}
}

/// <summary>
/// Configuration Settings
/// </summary>
class GlsSettings : TspConfigurationBase
{
	public double Optima { get; set; }	
}
