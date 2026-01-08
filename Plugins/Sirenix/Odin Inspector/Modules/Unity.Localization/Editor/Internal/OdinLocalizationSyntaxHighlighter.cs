//-----------------------------------------------------------------------
// <copyright file="OdinLocalizationSyntaxHighlighter.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Text;
using Sirenix.OdinInspector.Modules.Localization.Editor.Configs;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.SmartFormat;
using UnityEngine.Localization.SmartFormat.Core.Parsing;
using UnityEngine.Localization.SmartFormat.Core.Settings;

namespace Sirenix.OdinInspector.Modules.Localization.Editor.Internal
{
	public static class OdinLocalizationSyntaxHighlighter
	{
		private const string END_RICH_COLOR = "</color>";

		private static readonly SmartFormatter Formatter;
		
		private static readonly StringBuilder Buffer;

		// TODO: if this works nicely, maybe implement a custom solution that doesn't rely on SmartFormatter
		
		static OdinLocalizationSyntaxHighlighter()
		{
			Formatter = Smart.CreateDefaultSmartFormat();
			Formatter.Settings.ParseErrorAction = ErrorAction.MaintainTokens;
			Formatter.Settings.FormatErrorAction = ErrorAction.MaintainTokens;
			
			Buffer = new StringBuilder();
		}

		public static string HighlightAsRichText(string source)
		{
			Format format;

			try
			{
				format = Formatter.Parser.ParseFormat(source, Formatter.GetNotEmptyFormatterExtensionNames());
			}
			catch (Exception)
			{
				return source;
			}

			Buffer.Clear();

			int expectedSize = source.Length;
			AppendToBuffer(format, source, ref expectedSize);

			// NOTE: we fallback to source in-case any obvious discrepancies are happening.
			return Buffer.Length != expectedSize ? source : Buffer.ToString();
		}

		public static string GetErrorMessage(string source, out bool foundError, out Exception exception)
		{
			Formatter.Settings.ParseErrorAction = ErrorAction.OutputErrorInResult;
			Formatter.Settings.FormatErrorAction = ErrorAction.OutputErrorInResult;

			Format format;

			try
			{
				format = Formatter.Parser.ParseFormat(source, Formatter.GetNotEmptyFormatterExtensionNames());
			}
			catch (Exception e)
			{
				foundError = true;

				exception = e;

				Debug.LogException(e);

				return $"Unity Formatter threw {ObjectNames.NicifyVariableName(e.GetType().GetNiceName())}: '{e.Message}'\nCheck the console for more information.";
			}

			exception = null;

			Buffer.Clear();

			int expectedSize = source.Length;
			AppendToBuffer(format, source, ref expectedSize);

			foundError = expectedSize != Buffer.Length;

			Formatter.Settings.ParseErrorAction = ErrorAction.MaintainTokens;
			Formatter.Settings.FormatErrorAction = ErrorAction.MaintainTokens;

			return Buffer.ToString();
		}

		private static void AppendColorAsRichTag(Color color, ref int expectedSize)
		{
			var color32 = (Color32) color;

			var richTag = $"<color=#{color32.r:X2}{color32.g:X2}{color32.b:X2}>";
			expectedSize += richTag.Length;

			Buffer.Append(richTag);
		}

		private static bool IsLastBufferEqual(string expected)
		{
			if (expected.Length > Buffer.Length)
			{
				return false;
			}

			int bufferIndex = Buffer.Length - 1;

			for (int i1 = expected.Length - 1; i1 >= 0; i1--, bufferIndex--)
			{
				if (Buffer[bufferIndex] != expected[i1])
				{
					return false;
				}
			}

			return true;
		}

		private static void AppendToBuffer(Format format, string source, ref int expectedSize)
		{
			for (var i = 0; i < format.Items.Count; i++)
			{
				if (Buffer.Length == source.Length)
				{
					break;
				}
				
				FormatItem item = format.Items[i];

				if (!(item is Placeholder placeholder))
				{
					Buffer.Append(item.RawText);
					continue;
				}

				AppendColorAsRichTag(OdinLocalizationConfig.Instance.placeholderColor, ref expectedSize);

				Buffer.Append('{');

				for (var j = 0; j < placeholder.Selectors.Count; j++)
				{
					Selector selector = placeholder.Selectors[j];

					AppendColorAsRichTag(OdinLocalizationConfig.Instance.selectorColor, ref expectedSize);

					string op = selector.Operator;

					if (!string.IsNullOrEmpty(op))
					{
						Buffer.Append(selector.Operator);
					}

					Buffer.Append(selector.RawText);

					Buffer.Append(END_RICH_COLOR);
					expectedSize += END_RICH_COLOR.Length;
				}

				if (placeholder.Alignment != 0)
				{
					Buffer.Append(',');

					Buffer.Append(placeholder.Alignment);
				}

				if (!string.IsNullOrEmpty(placeholder.FormatterName))
				{
					Buffer.Append(':');

					AppendColorAsRichTag(OdinLocalizationConfig.Instance.formatterColor, ref expectedSize);

					Buffer.Append(placeholder.FormatterName);

					if (!string.IsNullOrEmpty(placeholder.FormatterOptions))
					{
						Buffer.Append('(');
						Buffer.Append(placeholder.FormatterOptions);
						Buffer.Append(')');
					}

					Buffer.Append(END_RICH_COLOR);
					expectedSize += END_RICH_COLOR.Length;
				}

				if (placeholder.Format != null)
				{
					Format nextFormat = placeholder.Format;

					bool showColon;

					if (nextFormat.startIndex > 0)
					{
						showColon = nextFormat.baseString[nextFormat.startIndex - 1] == ':';
					}
					else
					{
						showColon = true;
					}

					if (showColon)
					{
						Buffer.Append(':');
					}

					AppendToBuffer(nextFormat, source, ref expectedSize);
				}

				Buffer.Append('}');

				Buffer.Append(END_RICH_COLOR);
				expectedSize += END_RICH_COLOR.Length;
			}
		}
	}
}