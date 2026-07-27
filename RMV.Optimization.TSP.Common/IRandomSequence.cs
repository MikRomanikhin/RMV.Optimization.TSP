namespace RMV.Optimization.TSP.Common;

/// <summary>
/// Methods generating random sequences
/// </summary>
public interface IRandomSequence
{
	/// <summary>
	/// Generates an ordered list of random unique integers within a specified range
	/// </summary>
	/// <param name="count">Number of unique integers to generate.</param>
	/// <param name="min">Minimum value of the range (inclusive).</param>
	/// <param name="max">Maximum value of the range (inclusive).</param>
	public static List<int> GetUniqueInts( int count, int min, int max )
	{
		if( count > ( max - min + 1 ) ) throw new ArgumentException( "Length exceeds the range" );

		return [ .. Enumerable.Range( min, max - min ).OrderBy( _ => Random.Shared.Next() ).Take( count ).OrderBy( x => x ) ];
	}

	/// <summary>
	/// Generates a pair of unique integers within a specified range
	/// </summary>
	/// <param name="min">Minimum value of the range (inclusive).</param>
	/// <param name="max">Maximum value of the range (inclusive).</param>
	public static (int,int) GetPairInts( int min, int max )
	{
		if( max < min ) ( min, max) = (max, min);
		if( max - min < 2 ) throw new ArgumentException( "Range must be at least 2 units wide" );

		int result1 = Random.Shared.Next( min, max - 2 );
		int result2 = Random.Shared.Next( result1, max );		

		return (result1, result2);
	}
	
}
