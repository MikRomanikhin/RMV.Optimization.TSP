using RMV.Common.Configuration;

using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

public class GreedyCombined( TspMap map ) : AlgorithmBase( map ), ITspAsync
{
	IlsSettings settings;

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<IlsSettings>( "greedy" );
	}

	public async Task<TspResult> RunAsync()
	{
		base.timer.Start();

		int count = 0;
		int noChanges = 0;		

		var greedy = new NearestNeighbour( map );

		var best = await greedy.RunAsync();		

		await Task.Run( () => 
		{
			while( noChanges++ < settings.Limit )
			{
				IList<int> current = [ .. best.Path ];

				LocalSearch( current );

				//if( current < best ) best = new TspResult { Tour = current.Tour, Path = current.Path };

				if( ++count % settings.Redraw == 0 ) base.Draw( best.Tour, count, best.Path );				
			}

			base.Draw( best.Tour, count, best.Path );
		} );

		base.timer.Stop();

		return best;		
	}


	//TspResult LocalSearch( int[] path )
	//{
	//	int n = path.Length;

	//	int[] newPath = new int[ n ];

	//	double newTour = 0;
	//	double oldTour = base.map.GetTour( path );

	//	bool improved = true;

	//	while( improved )
	//	{
	//		improved = false;

	//		for( int i = 0; i < n - 1; i++ )
	//		{
	//			for( int j = i + 1; j < n; j++ )
	//			{
	//				newPath = TwoOptSwap( path, i, j );
	//				newTour = base.map.GetTour( newPath );

	//				if( newTour < oldTour )
	//				{
	//					oldTour = newTour;
	//					Array.Copy( newPath, path, n );
	//					improved = true;
	//				}
	//			}
	//		}
	//	}

	//	return new TspResult { Tour = newTour, Path = newPath }; ;
	//}
}
