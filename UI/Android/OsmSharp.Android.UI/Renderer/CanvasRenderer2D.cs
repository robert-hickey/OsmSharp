
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using OsmSharp.UI.Renderer;
using OsmSharp.UI.Renderer.Scene2DPrimitives;
using System.IO;

namespace OsmSharp.Android.UI
{
	/// <summary>
	/// Android canvas renderer.
	/// </summary>
	public class CanvasRenderer2D : Renderer2D<global::Android.Graphics.Canvas>
	{
		/// <summary>
		/// Holds the paint object.
		/// </summary>
		private global::Android.Graphics.Paint _paint;

		/// <summary>
		/// Initializes a new instance of the <see cref="OsmSharp.Android.UI.AndroidCanvasRenderer"/> class.
		/// </summary>
		public CanvasRenderer2D()
		{
			_paint = new global::Android.Graphics.Paint();
			_paint.AntiAlias= true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OsmSharp.Android.UI.CanvasRenderer2D"/> class.
		/// </summary>
		/// <param name="paint">Paint.</param>
		public CanvasRenderer2D(global::Android.Graphics.Paint paint)
		{
			_paint = paint;
		}

		
		#region Caching Implementation
		
		/// <summary>
		/// Called right before rendering starts.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="scenes"></param>
		/// <param name="view"></param>
		protected override void OnBeforeRender(Target2DWrapper<global::Android.Graphics.Canvas> target, List<Scene2D> scenes, View2D view)
		{
			// create a bitmap and render there.
			var bitmap = global::Android.Graphics.Bitmap.CreateBitmap((int)target.Width, (int)target.Height,
			                                                          global::Android.Graphics.Bitmap.Config.Argb8888);
			target.BackTarget = target.Target;
			target.Target = new global::Android.Graphics.Canvas(bitmap);
			target.Target.DrawColor(global::Android.Graphics.Color.Transparent);
			
			target.Tag = bitmap;
		}
		
		/// <summary>
		/// Called right after rendering.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="scenes"></param>
		/// <param name="view"></param>
		protected override void OnAfterRender(Target2DWrapper<global::Android.Graphics.Canvas> target, List<Scene2D> scenes, View2D view)
		{
			target.Target.Dispose(); // dispose of the old target.
			target.Target = target.BackTarget;
			var bitmap = target.Tag as global::Android.Graphics.Bitmap;
			if (bitmap != null)
			{
				target.Target.DrawBitmap(bitmap, 0, 0, _paint);
			}
		}
		
		/// <summary>
		/// Builds the cached scene.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="currentCache"></param>
		/// <param name="currentScenes"></param>
		/// <param name="view"></param>
		/// <returns></returns>
		protected override Scene2D BuildSceneCache(Target2DWrapper<global::Android.Graphics.Canvas> target, Scene2D currentCache, 
		                                           List<Scene2D> currentScenes, View2D view)
		{
			var scene = new Scene2D();
			scene.BackColor = currentScenes[0].BackColor;
			
			var bitmap = target.Tag as global::Android.Graphics.Bitmap;
			if (bitmap != null)
			{
				byte[] imageData = null;
				using (var stream = new MemoryStream())
				{
					bitmap.Compress(global::Android.Graphics.Bitmap.CompressFormat.Png, 0, stream);
					
					imageData = stream.ToArray();
				}
				scene.AddImage(0, float.MinValue, float.MaxValue, 
				               view.Left, view.Top, view.Right, view.Bottom, imageData);
				
				// dispose of bitmap.
				bitmap.Dispose();
			}
			return scene;
		}
		
		#endregion

		#region implemented abstract members of Renderer2D

		/// <summary>
		/// Returns the target wrapper.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public override Target2DWrapper<global::Android.Graphics.Canvas> CreateTarget2DWrapper (global::Android.Graphics.Canvas target)
		{
			return new Target2DWrapper<global::Android.Graphics.Canvas>(
				target, target.Width, target.Height);
		}

		/// <summary>
		/// Holds the view.
		/// </summary>
		private View2D _view;

		/// <summary>
		/// Holds the target.
		/// </summary>
		private Target2DWrapper<global::Android.Graphics.Canvas> _target;

		/// <summary>
		/// Transforms the canvas to the coordinate system of the view.
		/// </summary>
		/// <param name="view">View.</param>
		protected override void Transform (Target2DWrapper<global::Android.Graphics.Canvas> target, View2D view)
		{
			_view = view;
			_target = target;
		}
		
		/// <summary>
		/// Transforms the x-coordinate to screen coordinates.
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		private float TransformX(double x)
		{
			return (float)_view.ToViewPortX(_target.Width, x);
		}
		
		/// <summary>
		/// Transforms the y-coordinate to screen coordinates.
		/// </summary>
		/// <param name="y"></param>
		/// <returns></returns>
		private float TransformY(double y)
		{
			return (float)_view.ToViewPortY(_target.Height, y);
		}
		
		/// <summary>
		/// Returns the size in pixels.
		/// </summary>
		/// <returns></returns>
		private float ToPixels(double sceneSize)
		{
			double scaleX = _target.Width / _view.Width;
			
			return (float)(sceneSize * scaleX);
		}
		
		/// <summary>
		/// Returns the size in pixels.
		/// </summary>
		/// <returns>The pixels.</returns>
		/// <param name="target"></param>
		/// <param name="view">View.</param>
		/// <param name="sizeInPixels">Size in pixels.</param>
		protected override double FromPixels(Target2DWrapper<global::Android.Graphics.Canvas> target, View2D view, double sizeInPixels)
		{
			double scaleX = target.Width / view.Width;
			
			return sizeInPixels / scaleX;
		}

		/// <summary>
		/// Draws the backcolor.
		/// </summary>
		/// <param name="backColor"></param>
		protected override void DrawBackColor (Target2DWrapper<global::Android.Graphics.Canvas> target, int backColor)
		{
			target.Target.DrawColor(new global::Android.Graphics.Color(backColor));
		}

		/// <summary>
		/// Draws a point on the target. The coordinates given are scene coordinates.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="color">Color.</param>
		/// <param name="size">Size.</param>
		protected override void DrawPoint (Target2DWrapper<global::Android.Graphics.Canvas> target, double x, double y, int color, double size)
		{
			float sizeInPixels = this.ToPixels(size);
			_paint.Color = new global::Android.Graphics.Color(color);
			_paint.StrokeWidth = 1;
			_paint.SetStyle(global::Android.Graphics.Paint.Style.Fill);

			target.Target.DrawCircle(this.TransformX(x), this.TransformY(y), sizeInPixels,
			                  _paint);
		}

		/// <summary>
		/// Draws a line on the target. The coordinates given are scene coordinates.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="color">Color.</param>
		/// <param name="width">Width.</param>
		protected override void DrawLine (Target2DWrapper<global::Android.Graphics.Canvas> target, double[] x, double[] y, 
		                                  int color, double width, LineJoin lineJoine, int[] dashes)
		{
			if(x.Length > 1)
			{
				float widthInPixels = this.ToPixels(width);

				_paint.Color = new global::Android.Graphics.Color(color);
				_paint.StrokeWidth = widthInPixels;

				// convert to the weid android api array!
				float[] lineCoordinates = new float[(x.Length - 2) * 4 + 4];
				lineCoordinates[0] = this.TransformX(x[0]);
				lineCoordinates[1] = this.TransformY(y[0]);
				for(int idx = 1; idx < x.Length - 1; idx++)
				{					
					int androidApiIndex = (idx - 1) * 4 + 2;
					lineCoordinates[androidApiIndex] = this.TransformX(x[idx]);
					lineCoordinates[androidApiIndex + 1] = this.TransformY(y[idx]);
					lineCoordinates[androidApiIndex + 2] = this.TransformX(x[idx]);
					lineCoordinates[androidApiIndex + 3] = this.TransformY(y[idx]);
				}
				lineCoordinates[lineCoordinates.Length - 2] = this.TransformX(x[x.Length - 1]);
				lineCoordinates[lineCoordinates.Length - 1] = this.TransformY(y[y.Length - 1]);
				target.Target.DrawLines(lineCoordinates, 0, lineCoordinates.Length, _paint);
			}
		}

		/// <summary>
		/// Draws a polygon on the target. The coordinates given are scene coordinates.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="color">Color.</param>
		/// <param name="width">Width.</param>
		/// <param name="fill">If set to <c>true</c> fill.</param>
		protected override void DrawPolygon (Target2DWrapper<global::Android.Graphics.Canvas> target, double[] x, double[] y, 
		                                     int color, double width, bool fill)
		{
			if(x.Length > 1)
			{
				_paint.Color = new global::Android.Graphics.Color(color);
				_paint.StrokeWidth = (float)width;
				if(fill)
				{
					_paint.SetStyle(global::Android.Graphics.Paint.Style.Fill);
				}
				else					
				{
					_paint.SetStyle(global::Android.Graphics.Paint.Style.Stroke);
				}

				// convert android path object.
				global::Android.Graphics.Path path = new global::Android.Graphics.Path();
				path.MoveTo(this.TransformX(x[0]), this.TransformY(y[0]));
				for(int idx = 1; idx < x.Length; idx++)
				{
					path.LineTo(this.TransformX(x[idx]), this.TransformY(y[idx]));
				}
				path.LineTo(this.TransformX(x[0]), this.TransformY(y[0]));

				target.Target.DrawPath(path, _paint);
			}
		}

		/// <summary>
		/// Draws an icon on the target unscaled but centered at the given scene coordinates.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="imageData"></param>
		protected override void DrawIcon (Target2DWrapper<global::Android.Graphics.Canvas> target, double x, double y, 
		                                  byte[] imageData)
		{
			global::Android.Graphics.Bitmap image = global::Android.Graphics.BitmapFactory.DecodeByteArray(
				imageData, 0, imageData.Length);

//			// calculate target rectangle.
//			double sceneSizeX = this.FromPixels(target, _view, image.Width) / 2.0f;
//			double sceneSizeY = this.FromPixels(target, _view, image.Height) / 2.0f;
//			double sceneTop = x + sceneSizeX;
//			double sceneBottom = x - sceneSizeX;
//			double sceneLeft = y - sceneSizeY;
//			double sceneRight = y + sceneSizeY;

			target.Target.DrawBitmap(image, this.TransformX(x), this.TransformY(y), null);
			image.Dispose();
		}

		/// <summary>
		/// Draws an image on the target.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="left"></param>
		/// <param name="top"></param>
		/// <param name="right"></param>
		/// <param name="bottom"></param>
		/// <param name="imageData"></param>
		/// <param name="tag"></param>
		protected override object DrawImage (Target2DWrapper<global::Android.Graphics.Canvas> target, 
		                                     double left, double top, double right, double bottom, byte[] imageData,
		                                   object tag)
		{
			global::Android.Graphics.Bitmap image = (tag as global::Android.Graphics.Bitmap);
			if(image == null)
			{
				image = global::Android.Graphics.BitmapFactory.DecodeByteArray(
					imageData, 0, imageData.Length);
			}
			
			float leftPixels = this.TransformX(left);
			float topPixels = this.TransformY(top);
			float rightPixels = this.TransformX(right);
			float bottomPixels = this.TransformY(bottom);

			target.Target.DrawBitmap(image,new global::Android.Graphics.Rect(0, 0, image.Width, image.Height),
			                         new global::Android.Graphics.RectF(leftPixels, topPixels, rightPixels, bottomPixels),
			                         null);
			return image;
		}

		protected override void DrawText (Target2DWrapper<global::Android.Graphics.Canvas> target, double x, double y, 
		                                  string text, double size)
		{

		}

		#endregion
	}
}