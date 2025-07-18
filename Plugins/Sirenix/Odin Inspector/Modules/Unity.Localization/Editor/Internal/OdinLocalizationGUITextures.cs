//-----------------------------------------------------------------------
// <copyright file="OdinLocalizationGUITextures.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Sirenix.OdinInspector.Modules.Localization.Editor.Internal
{
	public static class OdinLocalizationGUITextures
	{
		private const float FADE_STRENGTH = 0.4f;
		
		public static Texture2D LeftToRightFade
		{
			get
			{
				if (leftToRightFade != null)
				{
					return leftToRightFade;
				}

				leftToRightFade = new Texture2D(32, 32)
				{
					hideFlags = HideFlags.HideAndDontSave
				};

				var pixels = new Color[32 * 32];

				var index = 0;

				for (var x = 0; x < 32; x++)
				{
					for (var y = 0; y < 32; y++)
					{
						pixels[index++] = Color.Lerp(Color.white, Color.clear, Mathf.Pow(y / 31.0f, FADE_STRENGTH));
					}
				}

				leftToRightFade.SetPixels(pixels);

				leftToRightFade.Apply();
					
				CleanupUtility.DestroyObjectOnAssemblyReload(leftToRightFade);

				return leftToRightFade;
			}
		}

		public static Texture2D TopToBottomFade
		{
			get
			{
				if (topToBottomFade != null)
				{
					return topToBottomFade;
				}

				topToBottomFade = new Texture2D(32, 32)
				{
					hideFlags = HideFlags.HideAndDontSave
				};

				var pixels = new Color[32 * 32];

				var index = 0;

				for (var x = 0; x < 32; x++)
				{
					for (var y = 0; y < 32; y++)
					{
						pixels[index++] = Color.Lerp(Color.white, Color.clear, Mathf.Pow(1.0f - x / 31.0f, FADE_STRENGTH));
					}
				}

				topToBottomFade.SetPixels(pixels);

				topToBottomFade.Apply();

				CleanupUtility.DestroyObjectOnAssemblyReload(topToBottomFade);

				return topToBottomFade;
			}
		}

		public static Texture2D BottomToTopFade
		{
			get
			{
				if (bottomToTopFade != null)
				{
					return bottomToTopFade;
				}

				bottomToTopFade = new Texture2D(32, 32)
				{
					hideFlags = HideFlags.HideAndDontSave
				};

				var pixels = new Color[32 * 32];

				var index = 0;

				for (var x = 0; x < 32; x++)
				{
					for (var y = 0; y < 32; y++)
					{
						pixels[index++] = Color.Lerp(Color.white, Color.clear, Mathf.Pow(x / 31.0f, FADE_STRENGTH));
					}
				}

				bottomToTopFade.SetPixels(pixels);

				bottomToTopFade.Apply();

				CleanupUtility.DestroyObjectOnAssemblyReload(bottomToTopFade);

				return bottomToTopFade;
			}
		}

		public static Texture2D RoundBlur6
		{
			get
			{
				if (roundBlur6 == null)
				{
					const string BASE64 =
						"iVBORw0KGgoAAAANSUhEUgAAAGYAAABmCAYAAAA53+RiAAAFDElEQVR4Ae2dzZIURRSFaQFBDGBBsHGB76Ju0BfxkXwRZaXvIgs3BhEiIT/DYHu+pG+R3XN6mDa6uZuTEacr61ZWVcb3kVXFaq5dSwuBELg6gVUN" +
						"Xa/X1a1tHWO7G8bU8RqfrSdQYNnuhjPq+Dh7tXqP9cbY2/4p4J+pTK5PqRrbGqdu2iUEAP/vlHfqV6rO6VuCdsUAm5SQm+p/vgl9wjklSN3RIqlIvN/OkAv+uQ693eRMW8I+khhDW87bFcNBoFO/pdxW7ihfbML+" +
						"LCdCBOSSVqulpLzW2FebvNSW/TcKxxG0tFnMvFKQgpC7yn3l3qZPreTkcSYYH2klhpWBBGS8UJ4r/AOHIY1xNZbtWBlsqzGQE4CPlAfKQ+Vb5XvlkcKjLe1wAjy6nio/K78pcKbxGKvHWT3SPrzA9VWGFKCzKlgl" +
						"SPlK+VF5rKQdj8ATXeon5Q/lmcIKYjWd6atsyKmlpNqQxBcYcninIOcbJVIE4cgNpjyFYAxrmMN+eWc7MSwxBvMo+0FJOw0BXg0whjXMLxWDqHrH8Eh7pKSdhgBsYWw/pnZXDPt8qZUclljaaQjAtqTAHPb2Ucbt" +
						"OTjLoZZ2OgIsgJIC96XNO9iq/8uUoGVgOichUJzZFv9xo1nMfOetQfOB9I9KYC/nfWKOevdc7HACTgwW0z4tgQvMnZhPO6XczRKIGIulvxgx/Q7sDCLGYukvRky/AzuDiLFY+osR0+/AziBiLJb+YsT0O7AziBiL" +
						"pb8YMf0O7AwixmLpL0ZMvwM7g4ixWPqLEdPvwM4gYiyW/mLE9DuwM4gYi6W/GDH9DuwMIsZi6S9GTL8DO4OIsVj6ixHT78DOIGIslv5ixPQ7sDOIGIulvxgx/Q7sDCLGYukvRky/AzuDiLFY+osR0+/AziBiLJb+" +
						"YsT0O7AziBiLpb8YMf0O7AwixmLpL0ZMvwM7g4ixWPqLEdPvwM4gYiyW/mLE9DuwM4gYi6W/GDH9DuwMIsZi6S9GTL8DO4OIsVj6ixHT78DOIGIslv5ixPQ7sDOIGIulvxgx/Q7sDCLGYukvRky/AzuDiLFY+osR" +
						"0+/AziBiLJb+YsT0O7AziBiLpb8YMf0O7AwixmLpL0ZMvwM7g4ixWPqLEdPvwM4gYiyW/mLE9DuwM4gYi6W/GDH9DuwMnJjxZ8vt6BRPReACcyfmVDfPdQ8gsE8MBi9YPOC6GXo1Ans5z2JqEH9PvnK1y2fU/yVQ" +
						"nNkW/3GtWQyFGniu/tsxIj+nJABjWBf35V6zGIwxoKS8Vv9sGZnOsQnAFsazHByM5sQwkBNeKk/HqPycggBsYVxy6nE27rUr5p2qiHmlvFB+UdJOQwC2MIY1zGG/d8VwkCXG4L+VX5UnStpxCcAUtjCGNcy3xCx/" +
						"fHm9XtO/odxSvlTuKw+Uh8p3ymPla+WmknY4AVbF7worBSl/Ks+U58o/yhvlfLVajVWzK4ZHG3JuK3eUuwqC7m361DiGHMYu56ufdpEAkHl3zO9tHl+sFITQr/fM+DorMUioVhdhSWGPVhfF6F9KSeG8iBGEj7Ri" +
						"On/p8ugiJQTWMN96+c9idGw0BnAhLsoJ9THAKiElZf5wyMoRmKnBrho8iyksCe8UQr+kqPuhLUD1jqlq1QBPrk+pGtsap27aJQRq1ZQgRFSqxulDgB5l41IL3EnMOKCfOsZ2N4yp4/TT9hOof/Fsd8NZdXxcocSM" +
						"nfyEQAhckcB/Z2rwteNGzXsAAAAASUVORK5CYII=";

					byte[] bytes = Convert.FromBase64String(BASE64);

					roundBlur6 = TextureUtilities.LoadImage(102, 102, bytes);

					roundBlur6.hideFlags = HideFlags.HideAndDontSave;

					CleanupUtility.DestroyObjectOnAssemblyReload(roundBlur6);
				}

				return roundBlur6;
			}
		}

		public static Texture2D RoundBlur20
		{
			get
			{
				if (roundBlur20 == null)
				{
					const string BASE64 =
						"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";

					byte[] bytes = Convert.FromBase64String(BASE64);

					roundBlur20 = TextureUtilities.LoadImage(193, 193, bytes);

					roundBlur20.hideFlags = HideFlags.HideAndDontSave;

					CleanupUtility.DestroyObjectOnAssemblyReload(roundBlur20);
				}

				return roundBlur20;
			}
		}
		
		private static Texture2D leftToRightFade;
		private static Texture2D topToBottomFade;
		private static Texture2D bottomToTopFade;
		private static Texture2D roundBlur6;
		private static Texture2D roundBlur20;
		
		public static void Clear()
		{
			if (leftToRightFade != null)
			{
				UnityEngine.Object.Destroy(leftToRightFade);
			}

			if (topToBottomFade != null)
			{
				UnityEngine.Object.Destroy(topToBottomFade);
			}

			if (bottomToTopFade != null)
			{
				UnityEngine.Object.Destroy(bottomToTopFade);
			}
		}
	}
}