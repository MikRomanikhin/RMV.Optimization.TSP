namespace RMV.Optimization.TSP.Common;

/// <summary>
/// Methods generating random sequences
/// </summary>
public interface IRandomSequence
{
	/// <summary>
	/// Gets an integer array with values between minimum value (inclusive) and maximum value (exclusive).
	/// </summary>
	/// <returns>The integer array.</returns>
	/// <param name="length">The array length</param>
	/// <param name="min">Minimum value (inclusive).</param>
	/// <param name="max">Maximum value (exclusive).</param>
	//public static int[] GetInts( int length, int min, int max ) =>
	//	Enumerable.Range( min, max - min ).OrderBy( x => Random.Shared.Next() ).Take( length ).ToArray();


	/// <summary>
	/// Generates an ordered list of random unique integers within a specified range
	/// </summary>		
	public static List<int> GetUniqueInts( int count, int min, int max )
	{
		if( count > ( max - min + 1 ) ) throw new ArgumentException( "Length exceeds the range" );

		return Enumerable.Range( min, max - min ).OrderBy( _ => Random.Shared.Next() ).Take( count ).OrderBy( x => x ).ToList();
	}

	/// <summary>
	/// Generates a pair of unique integers within a specified range
	/// </summary>	
	public static (int,int) GetPairInts( int min, int max )
	{
		if( max < min ) ( min, max) = (max, min);
		if( max - min < 2 ) throw new ArgumentException( "Range must be at least 2 units wide" );

		int result1 = Random.Shared.Next( min, max - 2 );
		int result2 = Random.Shared.Next( result1, max );		

		return (result1, result2);
	}

	/// <summary>
	/// Generates a list of unique ordered integers within a specified range
	/// </summary>	
	//public static List<int> GetUniqueOrderedInts( int length, int min, int max ) =>
	//	GetUniqueInts( length, min, max ).OrderBy( x => x ).ToList();
}
