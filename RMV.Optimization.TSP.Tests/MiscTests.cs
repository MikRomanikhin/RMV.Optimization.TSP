using RMV.Optimization.TSP.Common;

namespace RMV.Optimization.TSP.Tests;

public class MiscTests
{
	[Fact]
	public void Resize()
	{
		int[] a1 = [ 1, 2, 3 ];

		var a2 = a1;

		Array.Resize( ref a2, 5 );

		a1 = a2;

		Assert.Equal( 5, a1.Length );
	}

	[Fact]
	public void Reverse()
	{
		List<int> items = [ 0, 1, 2, 3, 4, 5, 6, 7, 8 ];
		items.Reverse( 3, 4 );
		Assert.Equal( [ 0,1,2,6,5,4,3,7,8 ], items );
	}

	[Fact]
	public void Sequence()
	{
		List<int> items1 = [ 0, 1, 2, 3, 4 ];
		List<int> items2 = [ 0, 1, 2, 4, 3 ];
		
		Assert.False( items1.SequenceEqual( items2 ));
	}

	[Fact]
	public void RandomSeq()
	{
		var seq = Enumerable.Range( 0, 10 ).OrderBy( _ => Random.Shared.Next() ).ToList();

		var t1 = IRandomSequence.GetUniqueInts(2, 0, 10 );

		var pair = IRandomSequence.GetPairInts( 0, 10 );

	}

	[Fact]
	public void Unique()
	{
		//var items1 = IRandomSequence;

		//Assert.False( items1.SequenceEqual( items2 ) );
	}

	[Fact]
	public void Copy()
	{
		List<int> parent = [ 0, 1, 2, 3, 4 ];
		//List<int> parent2 = [ 4, 3, 2, 1, 0 ];

		int p1 = 2; int p2 = 3;

		var path1 = parent[ p1..p2 ];

		Assert.Equal(path1, [2,3] );
	}

	[Fact]
	public void Wrap()
	{
		int count = 20;

		int[] items = new int[ count ];

		for( int i = 0; i < count; i++ )
		{
			items[ i ] = WrapIndex( i, count );
		}

		Assert.Equal( count, items.Length );
	}

	int WrapIndex( int index, int cities ) => ( ( index % cities ) + cities ) % cities;
}
