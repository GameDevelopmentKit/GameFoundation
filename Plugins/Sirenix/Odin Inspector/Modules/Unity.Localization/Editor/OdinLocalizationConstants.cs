//-----------------------------------------------------------------------
// <copyright file="OdinLocalizationConstants.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Sirenix.OdinInspector.Modules.Localization.Editor.Configs;

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
	public static class OdinLocalizationConstants
	{
		public static int AssetPreviewRowHeight => OdinLocalizationConfig.Instance.assetRowHeight;
		public const int DEFAULT_COLUMN_WIDTH = 300;
		public const int MIN_COLUMN_WIDTH = 200;
		public const int COLUMN_HEIGHT = 38;
		public const int ROW_MENU_WIDTH = 28;
		public const int ROW_HEIGHT = 30;
		public const int EMPTY_ASSET_ROW_HEIGHT = ROW_HEIGHT; //96;
		public const int ASSET_ROW_HEIGHT = 104;              //96;
		public const int ENTRY_PADDING = 4;
		public const int TOOLBAR_HEIGHT = 28;
		public const int DRAG_HANDLE_WIDTH = 27;
	}
}