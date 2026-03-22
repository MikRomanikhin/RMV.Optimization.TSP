using System.ComponentModel;
using System.Drawing.Drawing2D;

using RMV.Optimization.TSP.Common;

namespace RMV.Optimization.TSP.UI;

/// <summary>
/// Scales and renders map
/// </summary>
public class MapControl : Panel
{
   /// <summary>
   /// Required designer variable.
   /// </summary>
   Container components;

   // pens and brushes
   readonly Pen blackPen = new( Color.Black );
   readonly Brush whiteBrush = new SolidBrush( Color.White );

   const int MAX_X = 1000, MAX_Y = 1000;

   #region Properties -------------------------------------------------

   public int MaxX => MAX_X;  // X maximum value
	public int MaxY => MAX_Y;  // Y maximum value
	 
	IntRange rangeX = new( 0, MAX_X ); // X range	  
	IntRange rangeY = new( 0, MAX_Y ); // Y range


	/// <summary>
	/// TSP Map
	/// </summary>
	[DesignerSerializationVisibility( DesignerSerializationVisibility.Hidden )]
	public int[,] Map
   {
      get => map;
      set
      {
			if( !ReferenceEquals( map, value ) )
			{
				map = value;
				Invalidate();
			}
		}
   }
   int[,] map;

	/// <summary>
	/// TSP Path
	/// </summary>
	[DesignerSerializationVisibility( DesignerSerializationVisibility.Hidden )]
	public ushort[] Path
   {
      get => path;
      set
      {
         if( !ReferenceEquals( path, value ) )
         {
            path = value;
            Invalidate();
         }
      }
   }
   ushort[] path;

   /// <summary>
   /// Optimal Path
   /// </summary>
	[DesignerSerializationVisibility( DesignerSerializationVisibility.Hidden )]
	public ushort[] Optimal
   {
      get => opt;
      set
      {
         if( !ReferenceEquals( opt, value ) )
         {
            opt = value;
            Invalidate();
         }
      }
   }
   ushort[] opt;

	[DesignerSerializationVisibility( DesignerSerializationVisibility.Hidden )]
	public Color Color { get; set; } = Color.Blue;

   #endregion


   #region Initialize/Dispose --------------------------------------------

   /// <summary>
   /// Constructor
   /// </summary>
   public MapControl()
   {
      InitializeComponent();

      // Update control style
      SetStyle( ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.DoubleBuffer | ControlStyles.UserPaint, true );     
   }

   /// <summary>
   /// Clean up any resources being used.
   /// </summary>
   protected override void Dispose( bool disposing )
   {
      if( disposing )
      {
         components?.Dispose();         
         blackPen.Dispose();
         whiteBrush.Dispose();
      }

      base.Dispose( disposing );
   }

   #endregion`


   #region Component Designer generated code

   /// <summary>
   /// Required method for Designer support - do not modify 
   /// the contents of this method with the code editor.
   /// </summary>
   private void InitializeComponent()
   {
      components = new Container();
   }

	#endregion
	

	#region OnPaint ------------------------------------------------------------

	const int MARGIN = 6;

	protected override void OnPaint( PaintEventArgs pe )
   {
      Graphics g = pe.Graphics;
      g.SmoothingMode = SmoothingMode.HighSpeed;

      int clientWidth = ClientRectangle.Width;
      int clientHeight = ClientRectangle.Height;

      double xFactor = ( double )( clientWidth - 10 ) / rangeX.Length;
      double yFactor = ( double )( clientHeight - 10 ) / rangeY.Length;

      // fill with white background
      g.FillRectangle( whiteBrush, 0, 0, clientWidth - 1, clientHeight - 1 );

      // draw a black rectangle
      g.DrawRectangle( blackPen, 0, 0, clientWidth - 1, clientHeight - 1 );

      if( map != null ) // draw nodes
      {
         Brush brush = new SolidBrush( Color.Red );

         for( int i = 0; i < map.GetLength( 0 ); i++ )  // draw all points
         {
            int x = ( int )( ( map[ i, 0 ] - rangeX.Min ) * xFactor ) + 5;
            int y = clientHeight - MARGIN - ( int )( ( map[ i, 1 ] - rangeY.Min ) * yFactor );

            g.FillRectangle( brush, x - 2, y - 2, 5, 5 );
            //g.DrawString( i.ToString(), Font, Brushes.Black, x + 5, y - 2 ); // draw index
			}

         brush.Dispose();
      }

      if( Optimal != null && Optimal.Any() && map != null ) // draw path
      {
         DrawPath( Optimal, g, Color.Red, clientHeight, xFactor, yFactor );
      }

      if( path != null && path.Any() && map != null ) // draw path
      {
         DrawPath( path, g, Color.Blue, clientHeight, xFactor, yFactor );
      }

      base.OnPaint( pe );
   }
   

   void DrawPath( ushort[] path, Graphics g, Color color, int clientHeight, double xFactor, double yFactor )
   {
      var pen = new Pen( color, 1 );

      int prev = path[ ^1 ];

      int x1 = ( int )( ( map[ prev, 0 ] - rangeX.Min ) * xFactor ) + 5;
      int y1 = clientHeight - MARGIN - ( int )( ( map[ prev, 1 ] - rangeY.Min ) * yFactor );

      for( int i = 0; i < path.Length; i++ ) // connect all cities
      {
         int city = path[ i ]; //current city

         int x2 = ( int )( ( map[ city, 0 ] - rangeX.Min ) * xFactor ) + 5;
         int y2 = clientHeight - MARGIN - ( int )( ( map[ city, 1 ] - rangeY.Min ) * yFactor );

         g.DrawLine( pen, x1, y1, x2, y2 ); // connect previous city with the current one

         (x1, y1) = (x2, y2);
      }
   }

	#endregion

}
