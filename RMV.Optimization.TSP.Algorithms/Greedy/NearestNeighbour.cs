using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Nearest Neighbour TSP algorithm
/// </summary>
public class NearestNeighbour( TspMap map ) : TspAlgorithmBase( map ), ITspAsync
{	
	/// <summary>
	/// Applies NN algorithm to each starting node in the map
	/// </summary>	
	public async Task<TspResult> RunAsync(CancellationToken token )
	{
		var best = new TspResult( double.MaxValue, new List<int>( base.Cities ) );

		base.timer.Start();

		await Task.Run( () => 
		{			
			int count = 0;

			for( int city = 0; city < base.Cities; city++ )
			{		
				var result = GetNearest( city );

				if( result < best ) //tour length check
				{
					best = result;

					base.Draw(best.Tour, ++count, best.Path );					
				}
			}			
		} );

		base.timer.Stop();

		return best;		
	}

	#region obsolete
	/// <summary>
	/// Nearest neighbour algorithm for starting node
	/// </summary>
	/// <param name="start">starting node</param>	
	//public TspResult GetNearest( int start )
	//{
	//	var result = new TspResult();
	//	result.Add( start );

	//	int next = start;

	//	for( int step = 0; step < base.Cities; step++ )
	//	{
	//		var edge = map.FindEdges( next, result.Path ).MinBy( e => e.Weight ); //nearest non-visited edge

	//		if( edge == null ) //the last edge/node
	//		{
	//			result.Tour += map[ next, start ].Weight;

	//			return result;
	//		}

	//		result.Tour += edge.Weight; //edge.Visited = true;

	//		next = edge.Next; //next node on selected edge		

	//		result.Add( next );
	//	}

	//	throw new Exception( "OOPS!" );
	//}
	#endregion
}
