﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Threading;

namespace LaserGRBL.RasterConverter
{
	public partial class RasterToLaserForm : Form
	{
		ImageProcessor IP;
		bool preventClose;
		
		private RasterToLaserForm(GrblCore core, string filename)
		{
			InitializeComponent();

			IP = new ImageProcessor(core, filename, PbConverted.Size);
			PbOriginal.Image = IP.Original;
			ImageProcessor.PreviewReady += OnPreviewReady;
			ImageProcessor.PreviewBegin += OnPreviewBegin;
			ImageProcessor.GenerationComplete += OnGenerationComplete;
			
			LblGrayscale.Visible = CbMode.Visible = !IP.IsGrayScale;
			
			CbResize.SuspendLayout();
			CbResize.Items.Add(InterpolationMode.HighQualityBicubic);
			CbResize.Items.Add(InterpolationMode.NearestNeighbor);
			CbResize.ResumeLayout();
			CbMode.SuspendLayout();
			foreach (ImageTransform.Formula formula in Enum.GetValues(typeof(ImageTransform.Formula)))
				CbMode.Items.Add(formula);
			CbMode.SelectedIndex = 0;
			CbMode.ResumeLayout();
			CbDirections.SuspendLayout();
			foreach (ImageProcessor.Direction direction in Enum.GetValues(typeof(ImageProcessor.Direction)))
				if (direction != ImageProcessor.Direction.None)
					CbDirections.Items.Add(direction);
			CbDirections.SelectedIndex = 0;
			CbDirections.ResumeLayout();

			CbFillingDirection.SuspendLayout();
			foreach (ImageProcessor.Direction direction in Enum.GetValues(typeof(ImageProcessor.Direction)))
				CbFillingDirection.Items.Add(direction);
			CbFillingDirection.SelectedIndex = 0;
			CbFillingDirection.ResumeLayout();
			
			if (IP.Original.Height < IP.Original.Width)
			{
				IISizeW.CurrentValue = 50;
				IISizeH.CurrentValue = IP.WidthToHeight(50);
			}
			else
			{
				IISizeH.CurrentValue = 50;
				IISizeW.CurrentValue = IP.HeightToWidht(50);
			}
			LoadSettings();
		}
		
		void OnPreviewBegin()
		{
			preventClose = true;
				
			if (InvokeRequired)
			{
				Invoke(new ImageProcessor.PreviewBeginDlg(OnPreviewBegin));
			}
			else
			{
				WT.Enabled = true;
				BtnCreate.Enabled = false;				
			}
		}
		void OnPreviewReady(Image img)
		{
			if (InvokeRequired)
			{
				Invoke(new ImageProcessor.PreviewReadyDlg(OnPreviewReady), img);
			}
			else
			{
				Image old = PbConverted.Image;
				PbConverted.Image = img.Clone() as Image;
				if (old != null)
					old.Dispose();
				WT.Enabled = false;
				WB.Visible = false;
				WB.Running = false;
				BtnCreate.Enabled = true;
				preventClose = false;
			}
		}

		void OnGenerationComplete(Exception ex)
		{
			if (InvokeRequired)
			{
				Invoke(new ImageProcessor.GenerationCompleteDlg(OnGenerationComplete), ex);
			}
			else
			{
				Cursor = Cursors.Default;
				if (ex != null)
					System.Windows.Forms.MessageBox.Show(ex.Message);
				preventClose = false;
				Close();
			}
		}
		
		void WTTick(object sender, EventArgs e)
		{
			WT.Enabled = false;
			WB.Visible = true;
			WB.Running = true;
		}
		
		internal static void CreateAndShowDialog(GrblCore core, string filename)
		{
			using (RasterToLaserForm f = new RasterToLaserForm(core, filename))
				f.ShowDialog();
		}

		void GoodInput(object sender, KeyPressEventArgs e)
		{
			if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
				e.Handled = true;
		}

		void BtnCreateClick(object sender, EventArgs e)
		{
			preventClose = true;
			Cursor = Cursors.WaitCursor;
			SuspendLayout();
			TCOriginalPreview.SelectedIndex = 0;
			TCOptions.Enabled = false;
			FlipControl.Enabled = false;
			BtnCreate.Enabled = false;
			WB.Visible = true;
			WB.Running = true;
			ResumeLayout();
	
			StoreSettings();

			ImageProcessor targetProcessor = IP.Clone() as ImageProcessor;
			IP.GenerateGCode();
		}

		private void StoreSettings()
		{
			Settings.SetObject("GrayScaleConversion.RasterConversionTool", RbLineToLineTracing.Checked ? ImageProcessor.Tool.Line2Line : ImageProcessor.Tool.Vectorize);

			
			Settings.SetObject("GrayScaleConversion.Line2LineOptions.Direction", (ImageProcessor.Direction)CbDirections.SelectedItem);
			Settings.SetObject("GrayScaleConversion.Line2LineOptions.Quality", UDQuality.Value);
			Settings.SetObject("GrayScaleConversion.Line2LineOptions.Preview", CbLinePreview.Checked);

			Settings.SetObject("GrayScaleConversion.VectorizeOptions.SpotRemoval.Enabled", CbSpotRemoval.Checked);
			Settings.SetObject("GrayScaleConversion.VectorizeOptions.SpotRemoval.Value", UDSpotRemoval.Value);
			Settings.SetObject("GrayScaleConversion.VectorizeOptions.Smooting.Enabled", CbSmoothing.Checked);
			Settings.SetObject("GrayScaleConversion.VectorizeOptions.Smooting.Value", UDSmoothing.Value);
			Settings.SetObject("GrayScaleConversion.VectorizeOptions.Optimize.Enabled", CbOptimize.Checked);
			Settings.SetObject("GrayScaleConversion.VectorizeOptions.Optimize.Value", UDOptimize.Value);
//			Settings.SetObject("GrayScaleConversion.VectorizeOptions.ShowDots.Enabled", CbShowDots.Checked);
//			Settings.SetObject("GrayScaleConversion.VectorizeOptions.ShowImage.Enabled", CbShowImage.Checked);
			Settings.SetObject("GrayScaleConversion.VectorizeOptions.FillingDirection", (ImageProcessor.Direction)CbFillingDirection.SelectedItem);
			Settings.SetObject("GrayScaleConversion.VectorizeOptions.FillingQuality", UDFillingQuality.Value);
			Settings.SetObject("GrayScaleConversion.VectorizeOptions.BorderSpeed", IIBorderTracing.CurrentValue);

			Settings.SetObject("GrayScaleConversion.Parameters.Interpolation", (InterpolationMode)CbResize.SelectedItem);
			Settings.SetObject("GrayScaleConversion.Parameters.Mode", (ImageTransform.Formula)CbMode.SelectedItem);
			Settings.SetObject("GrayScaleConversion.Parameters.R", TBRed.Value);
			Settings.SetObject("GrayScaleConversion.Parameters.G", TBGreen.Value);
			Settings.SetObject("GrayScaleConversion.Parameters.B", TBBlue.Value);
			Settings.SetObject("GrayScaleConversion.Parameters.Brightness", TbBright.Value);
			Settings.SetObject("GrayScaleConversion.Parameters.Contrast", TbContrast.Value);
			Settings.SetObject("GrayScaleConversion.Parameters.Threshold.Enabled", CbThreshold.Checked);
			Settings.SetObject("GrayScaleConversion.Parameters.Threshold.Value", TbThreshold.Value);

			Settings.SetObject("GrayScaleConversion.Gcode.Speed.Mark", IILinearFilling.CurrentValue);
			Settings.SetObject("GrayScaleConversion.Gcode.Speed.Travel", IITravelSpeed.CurrentValue);

			Settings.SetObject("GrayScaleConversion.Gcode.LaserOptions.LaserOn", TxtLaserOn.Text);
			Settings.SetObject("GrayScaleConversion.Gcode.LaserOptions.LaserOff", TxtLaserOff.Text);
			Settings.SetObject("GrayScaleConversion.Gcode.LaserOptions.PowerMin", IIMinPower.CurrentValue);
			Settings.SetObject("GrayScaleConversion.Gcode.LaserOptions.PowerMax", IIMaxPower.CurrentValue);

			Settings.Save(); // Saves settings in application configuration file
		}

		private void LoadSettings()
		{
			if ((IP.SelectedTool = (ImageProcessor.Tool)Settings.GetObject("GrayScaleConversion.RasterConversionTool", ImageProcessor.Tool.Line2Line)) == ImageProcessor.Tool.Line2Line)
				RbLineToLineTracing.Checked = true;
			else
				RbVectorize.Checked = true;

			CbDirections.SelectedItem = IP.LineDirection = (ImageProcessor.Direction)Settings.GetObject("GrayScaleConversion.Line2LineOptions.Direction", ImageProcessor.Direction.Horizontal);
			UDQuality.Value = IP.Quality = Convert.ToInt32(Settings.GetObject("GrayScaleConversion.Line2LineOptions.Quality", 3));
			CbLinePreview.Checked = IP.LinePreview = (bool)Settings.GetObject("GrayScaleConversion.Line2LineOptions.Preview", false);

			CbSpotRemoval.Checked = IP.UseSpotRemoval = (bool)Settings.GetObject("GrayScaleConversion.VectorizeOptions.SpotRemoval.Enabled", false);
			UDSpotRemoval.Value = IP.SpotRemoval = (decimal)Settings.GetObject("GrayScaleConversion.VectorizeOptions.SpotRemoval.Value", 2.0m);
			CbSmoothing.Checked = IP.UseSmoothing = (bool)Settings.GetObject("GrayScaleConversion.VectorizeOptions.Smooting.Enabled", false);
			UDSmoothing.Value = IP.Smoothing = (decimal)Settings.GetObject("GrayScaleConversion.VectorizeOptions.Smooting.Value", 1.0m);
			CbOptimize.Checked = IP.UseOptimize = (bool)Settings.GetObject("GrayScaleConversion.VectorizeOptions.Optimize.Enabled", false);
			UDOptimize.Value = IP.Optimize = (decimal)Settings.GetObject("GrayScaleConversion.VectorizeOptions.Optimize.Value", 0.2m);
			//CbShowDots.Checked = IP.ShowDots = (bool)Settings.GetObject("GrayScaleConversion.VectorizeOptions.ShowDots.Enabled", false);
			//CbShowImage.Checked = IP.ShowImage = (bool)Settings.GetObject("GrayScaleConversion.VectorizeOptions.ShowImage.Enabled", true);
			CbFillingDirection.SelectedItem = IP.FillingDirection = (ImageProcessor.Direction)Settings.GetObject("GrayScaleConversion.VectorizeOptions.FillingDirection", ImageProcessor.Direction.None);
			UDFillingQuality.Value = IP.FillingQuality = Convert.ToInt32(Settings.GetObject("GrayScaleConversion.VectorizeOptions.FillingQuality", 3));
			IIBorderTracing.CurrentValue = IP.BorderSpeed = (int)Settings.GetObject("GrayScaleConversion.VectorizeOptions.BorderSpeed", 1000);

			CbResize.SelectedItem = IP.Interpolation = (InterpolationMode)Settings.GetObject("GrayScaleConversion.Parameters.Interpolation", InterpolationMode.HighQualityBicubic);
			CbMode.SelectedItem = IP.Formula = (ImageTransform.Formula)Settings.GetObject("GrayScaleConversion.Parameters.Mode", ImageTransform.Formula.SimpleAverage);
			TBRed.Value = IP.Red = (int)Settings.GetObject("GrayScaleConversion.Parameters.R", 100);
			TBGreen.Value = IP.Green = (int)Settings.GetObject("GrayScaleConversion.Parameters.G", 100);
			TBBlue.Value = IP.Blue = (int)Settings.GetObject("GrayScaleConversion.Parameters.B", 100);
			TbBright.Value = IP.Brightness = (int)Settings.GetObject("GrayScaleConversion.Parameters.Brightness", 100);
			TbContrast.Value = IP.Contrast = (int)Settings.GetObject("GrayScaleConversion.Parameters.Contrast", 100);
			CbThreshold.Checked = IP.UseThreshold = (bool)Settings.GetObject("GrayScaleConversion.Parameters.Threshold.Enabled", false);
			TbThreshold.Value = IP.Threshold = (int)Settings.GetObject("GrayScaleConversion.Parameters.Threshold.Value", 50);

			IILinearFilling.CurrentValue = IP.MarkSpeed = (int)Settings.GetObject("GrayScaleConversion.Gcode.Speed.Mark", 1000);
			IITravelSpeed.CurrentValue = IP.TravelSpeed = (int)Settings.GetObject("GrayScaleConversion.Gcode.Speed.Travel", 4000);

			TxtLaserOn.Text = IP.LaserOn = (string)Settings.GetObject("GrayScaleConversion.Gcode.LaserOptions.LaserOn", "M3");
			TxtLaserOff.Text = IP.LaserOff = (string)Settings.GetObject("GrayScaleConversion.Gcode.LaserOptions.LaserOff", "M5");
			IIMinPower.CurrentValue = IP.MinPower = (int)Settings.GetObject("GrayScaleConversion.Gcode.LaserOptions.PowerMin", 0);
			IIMaxPower.CurrentValue = IP.MaxPower = (int)Settings.GetObject("GrayScaleConversion.Gcode.LaserOptions.PowerMax", 255);
			
			UpdateSpeedEnabled();
		}

		private void IISizeW_CurrentValueChanged(object sender, int NewValue, bool ByUser)
		{
			if (ByUser)
				IISizeH.CurrentValue = IP.WidthToHeight(NewValue);
			
			IP.TargetSize = new Size(IISizeW.CurrentValue, IISizeH.CurrentValue);
		}

		private void IISizeH_CurrentValueChanged(object sender, int NewValue, bool ByUser)
		{
			if (ByUser)
				IISizeW.CurrentValue = IP.HeightToWidht(NewValue);
			
			IP.TargetSize = new Size(IISizeW.CurrentValue, IISizeH.CurrentValue);
		}

		void OnRGBCBDoubleClick(object sender, EventArgs e)
		{((UserControls.ColorSlider)sender).Value = 100;}

		void OnThresholdDoubleClick(object sender, EventArgs e)
		{((UserControls.ColorSlider)sender).Value = 50;}

		private void CbMode_SelectedIndexChanged(object sender, EventArgs e)
		{ 
			IP.Formula = (ImageTransform.Formula)CbMode.SelectedItem;

			SuspendLayout();
			TBRed.Visible = TBGreen.Visible = TBBlue.Visible = (IP.Formula == ImageTransform.Formula.Custom && !IP.IsGrayScale);
			LblRed.Visible = LblGreen.Visible = LblBlue.Visible = (IP.Formula == ImageTransform.Formula.Custom && !IP.IsGrayScale);
			ResumeLayout();
		}

		private void TBRed_ValueChanged(object sender, EventArgs e)
		{ IP.Red = TBRed.Value; }

		private void TBGreen_ValueChanged(object sender, EventArgs e)
		{ IP.Green = TBGreen.Value; }

		private void TBBlue_ValueChanged(object sender, EventArgs e)
		{ IP.Blue = TBBlue.Value; }

		private void TbBright_ValueChanged(object sender, EventArgs e)
		{ IP.Brightness = TbBright.Value; }

		private void TbContrast_ValueChanged(object sender, EventArgs e)
		{ IP.Contrast = TbContrast.Value; }

		private void CbThreshold_CheckedChanged(object sender, EventArgs e)
		{ 
			IP.UseThreshold = CbThreshold.Checked;
			TbThreshold.Visible = CbThreshold.Checked;
		}

		private void TbThreshold_ValueChanged(object sender, EventArgs e)
		{ IP.Threshold = TbThreshold.Value; }

		private void RbLineToLineTracing_CheckedChanged(object sender, EventArgs e)
		{
			if (RbLineToLineTracing.Checked)
				IP.SelectedTool = ImageProcessor.Tool.Line2Line;
			GbLineToLineOptions.Visible = RbLineToLineTracing.Checked;
			
			UpdateSpeedEnabled();
		}
		
		private void UpdateSpeedEnabled()
		{
			IILinearFilling.Enabled = LblLinearFilling.Enabled = LblLinearFillingmm.Enabled = (RbLineToLineTracing.Checked || (RbVectorize.Checked && ((ImageProcessor.Direction)CbFillingDirection.SelectedItem) != ImageProcessor.Direction.None));
			IIBorderTracing.Enabled = LblBorderTracing.Enabled = LblBorderTracingmm.Enabled = RbVectorize.Checked;
		}

		private void RbVectorize_CheckedChanged(object sender, EventArgs e)
		{
			if (RbVectorize.Checked)
				IP.SelectedTool = ImageProcessor.Tool.Vectorize;
			GbVectorizeOptions.Visible = RbVectorize.Checked;
			
			UpdateSpeedEnabled();
		}

		private void UDQuality_ValueChanged(object sender, EventArgs e)
		{ IP.Quality = (int)UDQuality.Value; }

		private void CbLinePreview_CheckedChanged(object sender, EventArgs e)
		{ IP.LinePreview = CbLinePreview.Checked; }

		private void UDSpotRemoval_ValueChanged(object sender, EventArgs e)
		{ IP.SpotRemoval = (int)UDSpotRemoval.Value; }

		private void CbSpotRemoval_CheckedChanged(object sender, EventArgs e)
		{ 
			IP.UseSpotRemoval = CbSpotRemoval.Checked;
			UDSpotRemoval.Enabled = CbSpotRemoval.Checked;
		}

		private void UDSmoothing_ValueChanged(object sender, EventArgs e)
		{ IP.Smoothing = UDSmoothing.Value; }

		private void CbSmoothing_CheckedChanged(object sender, EventArgs e)
		{ 
			IP.UseSmoothing = CbSmoothing.Checked;
			UDSmoothing.Enabled = CbSmoothing.Checked;
		}

		private void UDOptimize_ValueChanged(object sender, EventArgs e)
		{ IP.Optimize = UDOptimize.Value; }

		private void CbOptimize_CheckedChanged(object sender, EventArgs e)
		{
			IP.UseOptimize = CbOptimize.Checked;
			UDOptimize.Enabled = CbOptimize.Checked;
		}

//		private void CbShowDots_CheckedChanged(object sender, EventArgs e)
//		{ IP.ShowDots = CbShowDots.Checked; }
//
//		private void CbShowImage_CheckedChanged(object sender, EventArgs e)
//		{ IP.ShowImage = CbShowImage.Checked; }

		private void RasterToLaserForm_Load(object sender, EventArgs e)
		{IP.Resume();}
		
		void RasterToLaserFormFormClosing(object sender, FormClosingEventArgs e)
		{
			if (preventClose)
			{
				e.Cancel = true;
			}
			else
			{
				ImageProcessor.PreviewReady -= OnPreviewReady;
				ImageProcessor.PreviewBegin -= OnPreviewBegin;
				ImageProcessor.GenerationComplete -= OnGenerationComplete;
				IP.Dispose();
			}
		}

		void CbDirectionsSelectedIndexChanged(object sender, EventArgs e)
		{ IP.LineDirection = (ImageProcessor.Direction)CbDirections.SelectedItem; }

		void CbResizeSelectedIndexChanged(object sender, EventArgs e)
		{ 
			IP.Interpolation = (InterpolationMode)CbResize.SelectedItem;
			PbOriginal.Image = IP.Original;
		}
		void BtRotateCWClick(object sender, EventArgs e)
		{
			IP.RotateCW();
			PbOriginal.Image = IP.Original;
			
			int w = IISizeW.CurrentValue;
			int h = IISizeH.CurrentValue;
			
			IISizeW.CurrentValue = h;
			IISizeH.CurrentValue = w;
		}
		void BtRotateCCWClick(object sender, EventArgs e)
		{
			IP.RotateCCW();
			PbOriginal.Image = IP.Original;

			int w = IISizeW.CurrentValue;
			int h = IISizeH.CurrentValue;
			
			IISizeW.CurrentValue = h;
			IISizeH.CurrentValue = w;
			
		}
		void BtFlipHClick(object sender, EventArgs e)
		{
			IP.FlipH();
			PbOriginal.Image = IP.Original;
		}
		void BtFlipVClick(object sender, EventArgs e)
		{
			IP.FlipV();
			PbOriginal.Image = IP.Original;	
		}
		void IIMarkSpeedCurrentValueChanged(object sender, int NewValue, bool ByUser)
		{
			IP.MarkSpeed = NewValue;
		}
		void IITravelSpeedCurrentValueChanged(object sender, int NewValue, bool ByUser)
		{
			IP.TravelSpeed = NewValue;
		}
		void TxtLaserOnTextChanged(object sender, EventArgs e)
		{
			IP.LaserOn = TxtLaserOn.Text;
		}
		void TxtLaserOffTextChanged(object sender, EventArgs e)
		{
			IP.LaserOff = TxtLaserOff.Text;
		}
		void IIOffsetXYCurrentValueChanged(object sender, int NewValue, bool ByUser)
		{
			IP.TargetOffset = new Point(IIOffsetX.CurrentValue, IIOffsetY.CurrentValue);
		}
		void IIMinPowerCurrentValueChanged(object sender, int NewValue, bool ByUser)
		{
			IP.MinPower = NewValue;
		}
		void IIMaxPowerCurrentValueChanged(object sender, int NewValue, bool ByUser)
		{
			IP.MaxPower = NewValue;
		}

		private void CbFillingDirection_SelectedIndexChanged(object sender, EventArgs e)
		{
			IP.FillingDirection = (ImageProcessor.Direction)CbFillingDirection.SelectedItem;
			LblFillingLineLbl.Visible = LblFillingQuality.Visible = UDFillingQuality.Visible = ((ImageProcessor.Direction)CbFillingDirection.SelectedItem != ImageProcessor.Direction.None);
			
			UpdateSpeedEnabled();
		}

		private void UDFillingQuality_ValueChanged(object sender, EventArgs e)
		{
			IP.FillingQuality = (int)UDFillingQuality.Value;
		}
		
		
		bool isDrag = false;
  		Rectangle theRectangle = new Rectangle(new Point(0, 0), new Size(0, 0));
	  	Point startPoint;
		
		void PbConvertedMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button==MouseButtons.Left && Cropping)
			{
				isDrag = true;
				Control control = (Control) sender;
				startPoint = control.PointToScreen(new Point(e.X, e.Y));
			}
	
		}
		void PbConvertedMouseMove(object sender, MouseEventArgs e)
		{
			if (isDrag)
			{
				ControlPaint.DrawReversibleFrame(theRectangle, this.BackColor, FrameStyle.Dashed);
				
				// Calculate the endpoint and dimensions for the new 
				// rectangle, again using the PointToScreen method.
				Point endPoint = ((Control) sender).PointToScreen(new Point(e.X, e.Y));
				
				int width = endPoint.X-startPoint.X;
				int height = endPoint.Y-startPoint.Y;
				theRectangle = new Rectangle(startPoint.X, startPoint.Y, width, height);
				
				// Draw the new rectangle by calling DrawReversibleFrame
				// again.  
				ControlPaint.DrawReversibleFrame(theRectangle, this.BackColor, FrameStyle.Dashed);
			}
		}
		
		void PbConvertedMouseUp(object sender, MouseEventArgs e)
		{
			// If the MouseUp event occurs, the user is not dragging.
			if (isDrag)
			{
				isDrag = false;
				
				// Draw the rectangle to be evaluated. Set a dashed frame style 
				// using the FrameStyle enumeration.
				ControlPaint.DrawReversibleFrame(theRectangle, this.BackColor, FrameStyle.Dashed);
				
				// Find out which controls intersect the rectangle and 
				// change their color. The method uses the RectangleToScreen  
				// method to convert the Control's client coordinates 
				// to screen coordinates.
				Rectangle controlRectangle;
	//			for(int i = 0; i < Controls.Count; i++)
	//			{
	//				controlRectangle = Controls[i].RectangleToScreen(Controls[i].ClientRectangle);
	//				if (controlRectangle.IntersectsWith(theRectangle))
	//					Controls[i].BackColor = Color.BurlyWood;
	//			}
				
				// Reset the rectangle.
				theRectangle = new Rectangle(0, 0, 0, 0);
				Cropping = false;
				UpdateCropping();
			}
		}
		
		bool Cropping;
		void BtnCropClick(object sender, EventArgs e)
		{
			Cropping = !Cropping;
			UpdateCropping();
		}
		
		void UpdateCropping()
		{
			if (Cropping)
				BtnCrop.BackColor = Color.Orange;
			else
				BtnCrop.BackColor = DefaultBackColor;
		}
		void IIBorderTracingCurrentValueChanged(object sender, int NewValue, bool ByUser)
		{
			IP.BorderSpeed = NewValue;
		}
		
		
		
	}
}
