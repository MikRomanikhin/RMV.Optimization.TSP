namespace RMV.Optimization.TSP.UI
{
	partial class Form1
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if( disposing && ( components != null ) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			menuStrip1 = new MenuStrip();
			fileToolStripMenuItem = new ToolStripMenuItem();
			openToolStripMenuItem = new ToolStripMenuItem();
			GetMapMenuItem = new ToolStripMenuItem();
			GetOptimalMenuItem = new ToolStripMenuItem();
			resultToolStripMenuItem = new ToolStripMenuItem();
			saveToolStripMenuItem = new ToolStripMenuItem();
			computeToolStripMenuItem = new ToolStripMenuItem();
			NearestNeighborMenuItem = new ToolStripMenuItem();
			NearestNeighbourMenuItem = new ToolStripMenuItem();
			ShortestEdgeMenuItem = new ToolStripMenuItem();
			GreedyCombinedMenuItem = new ToolStripMenuItem();
			beamSearchMenuItem = new ToolStripMenuItem();
			PilotMenuItem = new ToolStripMenuItem();
			AnnealingMenuItem = new ToolStripMenuItem();
			EvolutionMenuItem = new ToolStripMenuItem();
			GeneticAlgorithmMenuItem = new ToolStripMenuItem();
			EvolutionStrategiesMenuItem = new ToolStripMenuItem();
			DifferentialEvolutionMenuItem = new ToolStripMenuItem();
			EvolutionaryProgrammingMenuItem = new ToolStripMenuItem();
			LearningClassifierMenuItem = new ToolStripMenuItem();
			stochasticToolStripMenuItem = new ToolStripMenuItem();
			iteratedLocalMenuItem = new ToolStripMenuItem();
			guidedLocalMenuItem = new ToolStripMenuItem();
			variableNeighborhoodpMenuItem = new ToolStripMenuItem();
			RandomizedAdaptiveMenuItem = new ToolStripMenuItem();
			ScatterSearchMenuItem = new ToolStripMenuItem();
			TabooSearchMenuItem = new ToolStripMenuItem();
			RandomSearchMenuItem = new ToolStripMenuItem();
			antsToolStripMenuItem = new ToolStripMenuItem();
			AntColonyMenuItem = new ToolStripMenuItem();
			AntSystemMenuItem = new ToolStripMenuItem();
			MinMaxAntMenuItem = new ToolStripMenuItem();
			PsoMenuItem = new ToolStripMenuItem();
			LearningMenuItem = new ToolStripMenuItem();
			statusStrip = new StatusStrip();
			StatusLabel = new ToolStripStatusLabel();
			tableLayoutPanel1 = new TableLayoutPanel();
			mapControl = new MapControl();
			tableLayoutPanel2 = new TableLayoutPanel();
			buttonPause = new Button();
			buttonStop = new Button();
			menuStrip1.SuspendLayout();
			statusStrip.SuspendLayout();
			tableLayoutPanel1.SuspendLayout();
			tableLayoutPanel2.SuspendLayout();
			SuspendLayout();
			// 
			// menuStrip1
			// 
			menuStrip1.Items.AddRange( new ToolStripItem[] { fileToolStripMenuItem, computeToolStripMenuItem } );
			menuStrip1.Location = new Point( 0, 0 );
			menuStrip1.Name = "menuStrip1";
			menuStrip1.Size = new Size( 1191, 24 );
			menuStrip1.TabIndex = 0;
			menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			fileToolStripMenuItem.DropDownItems.AddRange( new ToolStripItem[] { openToolStripMenuItem, saveToolStripMenuItem } );
			fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			fileToolStripMenuItem.Size = new Size( 37, 20 );
			fileToolStripMenuItem.Text = "File";
			// 
			// openToolStripMenuItem
			// 
			openToolStripMenuItem.DropDownItems.AddRange( new ToolStripItem[] { GetMapMenuItem, GetOptimalMenuItem, resultToolStripMenuItem } );
			openToolStripMenuItem.Name = "openToolStripMenuItem";
			openToolStripMenuItem.Size = new Size( 103, 22 );
			openToolStripMenuItem.Text = "Open";
			// 
			// GetMapMenuItem
			// 
			GetMapMenuItem.Name = "GetMapMenuItem";
			GetMapMenuItem.Size = new Size( 117, 22 );
			GetMapMenuItem.Text = "Map";
			GetMapMenuItem.Click +=  GetMapMenuItem_Click ;
			// 
			// GetOptimalMenuItem
			// 
			GetOptimalMenuItem.Name = "GetOptimalMenuItem";
			GetOptimalMenuItem.Size = new Size( 117, 22 );
			GetOptimalMenuItem.Text = "Optimal";
			GetOptimalMenuItem.Click +=  GetOptimalMenuItem_Click ;
			// 
			// resultToolStripMenuItem
			// 
			resultToolStripMenuItem.Name = "resultToolStripMenuItem";
			resultToolStripMenuItem.Size = new Size( 117, 22 );
			resultToolStripMenuItem.Text = "Result";
			// 
			// saveToolStripMenuItem
			// 
			saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			saveToolStripMenuItem.Size = new Size( 103, 22 );
			saveToolStripMenuItem.Text = "Save";
			// 
			// computeToolStripMenuItem
			// 
			computeToolStripMenuItem.DropDownItems.AddRange( new ToolStripItem[] { NearestNeighborMenuItem, AnnealingMenuItem, EvolutionMenuItem, stochasticToolStripMenuItem, antsToolStripMenuItem, LearningMenuItem } );
			computeToolStripMenuItem.Name = "computeToolStripMenuItem";
			computeToolStripMenuItem.Size = new Size( 69, 20 );
			computeToolStripMenuItem.Text = "Compute";
			// 
			// NearestNeighborMenuItem
			// 
			NearestNeighborMenuItem.DropDownItems.AddRange( new ToolStripItem[] { NearestNeighbourMenuItem, ShortestEdgeMenuItem, GreedyCombinedMenuItem, beamSearchMenuItem, PilotMenuItem } );
			NearestNeighborMenuItem.Name = "NearestNeighborMenuItem";
			NearestNeighborMenuItem.Size = new Size( 180, 22 );
			NearestNeighborMenuItem.Text = "Greedy";
			NearestNeighborMenuItem.Click +=  NearestNeighbourMenuItem_Click ;
			// 
			// NearestNeighbourMenuItem
			// 
			NearestNeighbourMenuItem.Name = "NearestNeighbourMenuItem";
			NearestNeighbourMenuItem.Size = new Size( 167, 22 );
			NearestNeighbourMenuItem.Text = "Nearest Neighbor";
			NearestNeighbourMenuItem.Click +=  NearestNeighbourMenuItem_Click ;
			// 
			// ShortestEdgeMenuItem
			// 
			ShortestEdgeMenuItem.Name = "ShortestEdgeMenuItem";
			ShortestEdgeMenuItem.Size = new Size( 167, 22 );
			ShortestEdgeMenuItem.Text = "Cheapest Insert";
			ShortestEdgeMenuItem.Click +=  ShortestEdgeMenuItem_Click ;
			// 
			// GreedyCombinedMenuItem
			// 
			GreedyCombinedMenuItem.Name = "GreedyCombinedMenuItem";
			GreedyCombinedMenuItem.Size = new Size( 167, 22 );
			GreedyCombinedMenuItem.Text = "Farthest Insert";
			GreedyCombinedMenuItem.Click +=  FarthestInsertMenuItem_Click ;
			// 
			// beamSearchMenuItem
			// 
			beamSearchMenuItem.Name = "beamSearchMenuItem";
			beamSearchMenuItem.Size = new Size( 167, 22 );
			beamSearchMenuItem.Text = "Beam Search";
			beamSearchMenuItem.Click +=  BeamSearchMenuItem_Click ;
			// 
			// PilotMenuItem
			// 
			PilotMenuItem.Name = "PilotMenuItem";
			PilotMenuItem.Size = new Size( 167, 22 );
			PilotMenuItem.Text = "Pilot Method";
			PilotMenuItem.Click +=  PilotMenuItem_Click ;
			// 
			// AnnealingMenuItem
			// 
			AnnealingMenuItem.Name = "AnnealingMenuItem";
			AnnealingMenuItem.Size = new Size( 180, 22 );
			AnnealingMenuItem.Text = "Annealing";
			AnnealingMenuItem.Click +=  Annealing_Click ;
			// 
			// EvolutionMenuItem
			// 
			EvolutionMenuItem.DropDownItems.AddRange( new ToolStripItem[] { GeneticAlgorithmMenuItem, EvolutionStrategiesMenuItem, DifferentialEvolutionMenuItem, EvolutionaryProgrammingMenuItem, LearningClassifierMenuItem } );
			EvolutionMenuItem.Name = "EvolutionMenuItem";
			EvolutionMenuItem.Size = new Size( 180, 22 );
			EvolutionMenuItem.Text = "Evolution";
			// 
			// GeneticAlgorithmMenuItem
			// 
			GeneticAlgorithmMenuItem.Name = "GeneticAlgorithmMenuItem";
			GeneticAlgorithmMenuItem.Size = new Size( 217, 22 );
			GeneticAlgorithmMenuItem.Text = "Genetic Algorithm";
			GeneticAlgorithmMenuItem.Click +=  GeneticAlgorithmMenuItem_Click ;
			// 
			// EvolutionStrategiesMenuItem
			// 
			EvolutionStrategiesMenuItem.Name = "EvolutionStrategiesMenuItem";
			EvolutionStrategiesMenuItem.Size = new Size( 217, 22 );
			EvolutionStrategiesMenuItem.Text = "Evolution Strategies";
			EvolutionStrategiesMenuItem.Click +=  EvolutionStrategiesMenuItem_Click ;
			// 
			// DifferentialEvolutionMenuItem
			// 
			DifferentialEvolutionMenuItem.Name = "DifferentialEvolutionMenuItem";
			DifferentialEvolutionMenuItem.Size = new Size( 217, 22 );
			DifferentialEvolutionMenuItem.Text = "Differential Evolution";
			DifferentialEvolutionMenuItem.Click +=  DifferentialEvolutionMenuItem_Click ;
			// 
			// EvolutionaryProgrammingMenuItem
			// 
			EvolutionaryProgrammingMenuItem.Name = "EvolutionaryProgrammingMenuItem";
			EvolutionaryProgrammingMenuItem.Size = new Size( 217, 22 );
			EvolutionaryProgrammingMenuItem.Text = "Evolutionary Programming";
			EvolutionaryProgrammingMenuItem.Click +=  EvolutionaryProgrammingMenuItem_Click ;
			// 
			// LearningClassifierMenuItem
			// 
			LearningClassifierMenuItem.Name = "LearningClassifierMenuItem";
			LearningClassifierMenuItem.Size = new Size( 217, 22 );
			LearningClassifierMenuItem.Text = "Learning Classifier";
			LearningClassifierMenuItem.Click +=  LearningClassifierMenuItem_Click ;
			// 
			// stochasticToolStripMenuItem
			// 
			stochasticToolStripMenuItem.DropDownItems.AddRange( new ToolStripItem[] { iteratedLocalMenuItem, guidedLocalMenuItem, variableNeighborhoodpMenuItem, RandomizedAdaptiveMenuItem, ScatterSearchMenuItem, TabooSearchMenuItem, RandomSearchMenuItem } );
			stochasticToolStripMenuItem.Name = "stochasticToolStripMenuItem";
			stochasticToolStripMenuItem.Size = new Size( 180, 22 );
			stochasticToolStripMenuItem.Text = "Stochastic";
			// 
			// iteratedLocalMenuItem
			// 
			iteratedLocalMenuItem.Name = "iteratedLocalMenuItem";
			iteratedLocalMenuItem.Size = new Size( 196, 22 );
			iteratedLocalMenuItem.Text = "Iterated Local Search";
			iteratedLocalMenuItem.Click +=  IteratedLocalSearch_Click ;
			// 
			// guidedLocalMenuItem
			// 
			guidedLocalMenuItem.Name = "guidedLocalMenuItem";
			guidedLocalMenuItem.Size = new Size( 196, 22 );
			guidedLocalMenuItem.Text = "Guided Local Search";
			guidedLocalMenuItem.Click +=  GuidedLocalSearch_Click ;
			// 
			// variableNeighborhoodpMenuItem
			// 
			variableNeighborhoodpMenuItem.Name = "variableNeighborhoodpMenuItem";
			variableNeighborhoodpMenuItem.Size = new Size( 196, 22 );
			variableNeighborhoodpMenuItem.Text = "Variable Neighborhood";
			variableNeighborhoodpMenuItem.Click +=  VariableNeighborhoodSearch_Click ;
			// 
			// RandomizedAdaptiveMenuItem
			// 
			RandomizedAdaptiveMenuItem.Name = "RandomizedAdaptiveMenuItem";
			RandomizedAdaptiveMenuItem.Size = new Size( 196, 22 );
			RandomizedAdaptiveMenuItem.Text = "GRASP";
			RandomizedAdaptiveMenuItem.Click +=  RandomizedAdaptiveSearch_Click ;
			// 
			// ScatterSearchMenuItem
			// 
			ScatterSearchMenuItem.Name = "ScatterSearchMenuItem";
			ScatterSearchMenuItem.Size = new Size( 196, 22 );
			ScatterSearchMenuItem.Text = "Scatter Search";
			ScatterSearchMenuItem.Click +=  ScatterSearchMenuItem_Click ;
			// 
			// TabooSearchMenuItem
			// 
			TabooSearchMenuItem.Name = "TabooSearchMenuItem";
			TabooSearchMenuItem.Size = new Size( 196, 22 );
			TabooSearchMenuItem.Text = "Tabu Search";
			TabooSearchMenuItem.Click +=  TabooSearchMenuItem_Click ;
			// 
			// RandomSearchMenuItem
			// 
			RandomSearchMenuItem.Name = "RandomSearchMenuItem";
			RandomSearchMenuItem.Size = new Size( 196, 22 );
			RandomSearchMenuItem.Text = "Random Search";
			RandomSearchMenuItem.Click +=  RandomSearchMenuItem_Click ;
			// 
			// antsToolStripMenuItem
			// 
			antsToolStripMenuItem.DropDownItems.AddRange( new ToolStripItem[] { AntColonyMenuItem, AntSystemMenuItem, MinMaxAntMenuItem, PsoMenuItem } );
			antsToolStripMenuItem.Name = "antsToolStripMenuItem";
			antsToolStripMenuItem.Size = new Size( 180, 22 );
			antsToolStripMenuItem.Text = "Swarm";
			// 
			// AntColonyMenuItem
			// 
			AntColonyMenuItem.Name = "AntColonyMenuItem";
			AntColonyMenuItem.Size = new Size( 152, 22 );
			AntColonyMenuItem.Text = "Ant Colony";
			AntColonyMenuItem.Click +=  AntSystemMenuItem_Click ;
			// 
			// AntSystemMenuItem
			// 
			AntSystemMenuItem.Name = "AntSystemMenuItem";
			AntSystemMenuItem.Size = new Size( 152, 22 );
			AntSystemMenuItem.Text = "Ant System";
			AntSystemMenuItem.Click +=  AntColonySystemMenuItem_Click ;
			// 
			// MinMaxAntMenuItem
			// 
			MinMaxAntMenuItem.Name = "MinMaxAntMenuItem";
			MinMaxAntMenuItem.Size = new Size( 152, 22 );
			MinMaxAntMenuItem.Text = "Min-Max Ant";
			MinMaxAntMenuItem.Click +=  MinMaxAntMenuItem_Click ;
			// 
			// PsoMenuItem
			// 
			PsoMenuItem.Name = "PsoMenuItem";
			PsoMenuItem.Size = new Size( 152, 22 );
			PsoMenuItem.Text = "Particle Swarm";
			PsoMenuItem.Click +=  PsoMenuItem_Click ;
			// 
			// LearningMenuItem
			// 
			LearningMenuItem.Name = "LearningMenuItem";
			LearningMenuItem.Size = new Size( 180, 22 );
			LearningMenuItem.Text = "Q-Learning";
			LearningMenuItem.Click +=  LearningMenuItem_Click ;
			// 
			// statusStrip
			// 
			statusStrip.Items.AddRange( new ToolStripItem[] { StatusLabel } );
			statusStrip.Location = new Point( 0, 906 );
			statusStrip.Name = "statusStrip";
			statusStrip.Size = new Size( 1191, 22 );
			statusStrip.TabIndex = 1;
			statusStrip.Text = "statusStrip1";
			// 
			// StatusLabel
			// 
			StatusLabel.Name = "StatusLabel";
			StatusLabel.Size = new Size( 126, 17 );
			StatusLabel.Text = "ia about to go running";
			// 
			// tableLayoutPanel1
			// 
			tableLayoutPanel1.BackColor = SystemColors.GradientActiveCaption;
			tableLayoutPanel1.ColumnCount = 2;
			tableLayoutPanel1.ColumnStyles.Add( new ColumnStyle( SizeType.Percent, 84.95174F ) );
			tableLayoutPanel1.ColumnStyles.Add( new ColumnStyle( SizeType.Percent, 15.0482655F ) );
			tableLayoutPanel1.Controls.Add( mapControl, 0, 0 );
			tableLayoutPanel1.Controls.Add( tableLayoutPanel2, 1, 0 );
			tableLayoutPanel1.Dock = DockStyle.Fill;
			tableLayoutPanel1.Location = new Point( 0, 24 );
			tableLayoutPanel1.Margin = new Padding( 10 );
			tableLayoutPanel1.Name = "tableLayoutPanel1";
			tableLayoutPanel1.RowCount = 1;
			tableLayoutPanel1.RowStyles.Add( new RowStyle( SizeType.Percent, 100F ) );
			tableLayoutPanel1.Size = new Size( 1191, 882 );
			tableLayoutPanel1.TabIndex = 2;
			// 
			// mapControl
			// 
			mapControl.Dock = DockStyle.Fill;
			mapControl.Location = new Point( 3, 3 );
			mapControl.Name = "mapControl";
			mapControl.Size = new Size( 1005, 876 );
			mapControl.TabIndex = 0;
			// 
			// tableLayoutPanel2
			// 
			tableLayoutPanel2.ColumnCount = 1;
			tableLayoutPanel2.ColumnStyles.Add( new ColumnStyle( SizeType.Percent, 50F ) );
			tableLayoutPanel2.Controls.Add( buttonPause, 0, 0 );
			tableLayoutPanel2.Controls.Add( buttonStop, 0, 1 );
			tableLayoutPanel2.Location = new Point( 1021, 10 );
			tableLayoutPanel2.Margin = new Padding( 10 );
			tableLayoutPanel2.Name = "tableLayoutPanel2";
			tableLayoutPanel2.RowCount = 2;
			tableLayoutPanel2.RowStyles.Add( new RowStyle( SizeType.Percent, 50F ) );
			tableLayoutPanel2.RowStyles.Add( new RowStyle( SizeType.Percent, 50F ) );
			tableLayoutPanel2.Size = new Size( 160, 100 );
			tableLayoutPanel2.TabIndex = 1;
			// 
			// buttonPause
			// 
			buttonPause.Dock = DockStyle.Fill;
			buttonPause.Font = new Font( "Segoe UI", 9F, FontStyle.Bold );
			buttonPause.ForeColor = SystemColors.ControlText;
			buttonPause.Location = new Point( 10, 10 );
			buttonPause.Margin = new Padding( 10 );
			buttonPause.Name = "buttonPause";
			buttonPause.Size = new Size( 140, 30 );
			buttonPause.TabIndex = 0;
			buttonPause.Text = "Pause";
			buttonPause.UseVisualStyleBackColor = true;
			buttonPause.Click +=  buttonPause_Click ;
			// 
			// buttonStop
			// 
			buttonStop.Dock = DockStyle.Fill;
			buttonStop.Font = new Font( "Segoe UI", 9F, FontStyle.Bold );
			buttonStop.Location = new Point( 10, 60 );
			buttonStop.Margin = new Padding( 10 );
			buttonStop.Name = "buttonStop";
			buttonStop.Size = new Size( 140, 30 );
			buttonStop.TabIndex = 1;
			buttonStop.Text = "Stop";
			buttonStop.UseVisualStyleBackColor = true;
			buttonStop.Click +=  buttonStop_Click ;
			// 
			// Form1
			// 
			AutoScaleDimensions = new SizeF( 7F, 15F );
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size( 1191, 928 );
			Controls.Add( tableLayoutPanel1 );
			Controls.Add( statusStrip );
			Controls.Add( menuStrip1 );
			MainMenuStrip = menuStrip1;
			Name = "Form1";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "TSP Algorithms Demo";
			menuStrip1.ResumeLayout( false );
			menuStrip1.PerformLayout();
			statusStrip.ResumeLayout( false );
			statusStrip.PerformLayout();
			tableLayoutPanel1.ResumeLayout( false );
			tableLayoutPanel2.ResumeLayout( false );
			ResumeLayout( false );
			PerformLayout();
		}

		#endregion

		private MenuStrip menuStrip1;
		private ToolStripMenuItem fileToolStripMenuItem;
		private ToolStripMenuItem openToolStripMenuItem;
		private ToolStripMenuItem GetMapMenuItem;
		private ToolStripMenuItem resultToolStripMenuItem;
		private ToolStripMenuItem saveToolStripMenuItem;
		private StatusStrip statusStrip;
		private TableLayoutPanel tableLayoutPanel1;
		
		private ToolStripMenuItem computeToolStripMenuItem;
		private ToolStripMenuItem NearestNeighborMenuItem;
		private ToolStripMenuItem EvolutionMenuItem;		
		private ToolStripStatusLabel StatusLabel;
		private ToolStripMenuItem GetOptimalMenuItem;
		private ToolStripMenuItem AnnealingMenuItem;
		private MapControl mapControl;
		private ToolStripMenuItem randomMenuItem;
		private ToolStripMenuItem NearestNeighbourMenuItem;
		private ToolStripMenuItem ShortestEdgeMenuItem;
		private ToolStripMenuItem stochasticToolStripMenuItem;
		private ToolStripMenuItem iteratedLocalMenuItem;
		private ToolStripMenuItem guidedLocalMenuItem;
		private ToolStripMenuItem variableNeighborhoodpMenuItem;
		private ToolStripMenuItem GreedyCombinedMenuItem;
		private ToolStripMenuItem RandomizedAdaptiveMenuItem;
		private ToolStripMenuItem antsToolStripMenuItem;
		private ToolStripMenuItem AntColonyMenuItem;
		private ToolStripMenuItem AntSystemMenuItem;
		private ToolStripMenuItem MinMaxAntMenuItem;
		private ToolStripMenuItem beamSearchMenuItem;
		private ToolStripMenuItem PilotMenuItem;
		private ToolStripMenuItem ScatterSearchMenuItem;
		private ToolStripMenuItem TabooSearchMenuItem;
		private ToolStripMenuItem PsoMenuItem;
		//private ToolStripMenuItem randomMenuItem;
		private ToolStripMenuItem GeneticAlgorithmMenuItem;
		private ToolStripMenuItem EvolutionStrategiesMenuItem;
		private ToolStripMenuItem DifferentialEvolutionMenuItem;
		private ToolStripMenuItem EvolutionaryProgrammingMenuItem;
		private ToolStripMenuItem LearningClassifierMenuItem;
		private ToolStripMenuItem LearningMenuItem;
		private TableLayoutPanel tableLayoutPanel2;
		private Button buttonPause;
		private Button buttonStop;
		private ToolStripMenuItem RandomSearchMenuItem;
	}
}
