using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Com.ForbiddenByte.OSA.Core;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView.Input
{
	public static class InputValidators
	{
		public delegate char StringValidator(string text, int charIndex, char addedChar);
	}
}